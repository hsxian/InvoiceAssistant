#!/usr/bin/env bash
# =============================================================================
# build-linux.sh —— 构建 InvoiceAssistant 的 Linux 版本（桌面端 + 控制台）
#
# App 用的是 Avalonia，跨平台，Linux 上可以直接跑（csproj 里的 OutputType=WinExe
# 和 app.manifest 只对 Windows 有意义，Linux 下由 SDK 忽略，产物仍是正常 ELF 可执行）。
# 桌面端和控制台共用同一套 Core / Paddle 模型 / 原生库 / config，所以发布到同一个
# 目录，只多出 20MB 左右的 Avalonia 依赖，而不是各自再复制一份 800MB 运行时。
#
# 用法：
#   ./build-linux.sh                        # 默认 Release / linux-x64 / 自包含（桌面端 + 控制台）
#   WITH_GUI=false ./build-linux.sh         # 只发控制台（老行为）
#   CONFIG=Debug ./build-linux.sh           # 调试构建
#   SELF_CONTAINED=false ./build-linux.sh   # 依赖系统已装 .NET 10 运行时
#   RID=linux-arm64 ./build-linux.sh        # 交叉发布 arm64
#   CLEAN=1 ./build-linux.sh                # 发布前先 dotnet clean
#
# 产物：InvoiceAssistant-publish-linux/
#   InvoiceAssistant.App       桌面端可执行文件
#   InvoiceAssistant.Console   控制台可执行文件
#   run-gui.sh                 启动桌面端（已处理原生库路径）
#   run.sh                     启动控制台（已处理原生库路径）
# =============================================================================
set -euo pipefail

# ---------- 可覆盖的参数 ----------
GUI_PROJECT="${GUI_PROJECT:-InvoiceAssistant.App}"
CLI_PROJECT="${CLI_PROJECT:-InvoiceAssistant.Console}"
WITH_GUI="${WITH_GUI:-true}"
OUT_DIR="${OUT_DIR:-InvoiceAssistant-publish-linux}"
RID="${RID:-linux-x64}"
CONFIG="${CONFIG:-Release}"
SELF_CONTAINED="${SELF_CONTAINED:-true}"
CLEAN="${CLEAN:-0}"

GUI_BIN="$GUI_PROJECT"
CLI_BIN="$CLI_PROJECT"

# ---------- 输出辅助 ----------
info() { printf '\033[1;36m==> %s\033[0m\n' "$*"; }
ok()   { printf '\033[1;32m  ✓ %s\033[0m\n' "$*"; }
warn() { printf '\033[1;33m  ! %s\033[0m\n' "$*"; }
die()  { printf '\033[1;31m  ✗ %s\033[0m\n' "$*" >&2; exit 1; }

# 只删自己产出的目录，绝不动别处
clean_dir() {
    local dst="${1:-}"
    case "$dst" in
        ""|"/"|"."|"..") die "拒绝删除非法路径：'$dst'" ;;
    esac
    [ -e "$dst" ] || return 0
    if [ -x /bin/rm ]; then /bin/rm -rf -- "$dst"; else rm -rf -- "$dst"; fi
}

# ---------- 0. 定位到仓库根目录 ----------
REPO_ROOT="$(cd "$(dirname "$(readlink -f "$0")")" && pwd)"
cd "$REPO_ROOT"

# OUT_DIR 允许写绝对路径（如 OUT_DIR=/tmp/out ./build-linux.sh），统一成绝对路径，
# 免得后面拼出 "仓库根//tmp/out" 这种畸形路径
case "$OUT_DIR" in
    /*) ;;
    *)  OUT_DIR="$REPO_ROOT/$OUT_DIR" ;;
esac

TMP_DIR="${OUT_DIR}-temp"
PUBLISH_LIST=()
[ "$WITH_GUI" = "true" ] && PUBLISH_LIST+=("$GUI_PROJECT")
PUBLISH_LIST+=("$CLI_PROJECT")

info "仓库根目录：$REPO_ROOT"
info "目标：${PUBLISH_LIST[*]}  RID=$RID  Configuration=$CONFIG  自包含=$SELF_CONTAINED"

# ---------- 1. 环境检查 ----------
command -v dotnet >/dev/null 2>&1 || die "未找到 dotnet CLI，请先安装 .NET SDK 9 或更高版本"
for p in "${PUBLISH_LIST[@]}"; do
    [ -f "$p/$p.csproj" ] || die "找不到项目文件：$p/$p.csproj"
done
ok "dotnet $(dotnet --version 2>/dev/null | head -1)"

if command -v gs >/dev/null 2>&1; then
    ok "Ghostscript $(gs --version 2>/dev/null)（扫描版 PDF 栅格化必需）"
else
    warn "未找到 Ghostscript（gs）。扫描版/图片型 PDF 会转换失败，纯文本层 PDF 不受影响。"
    warn "安装：sudo apt install ghostscript"
fi

if [ "$SELF_CONTAINED" != "true" ]; then
    if ! dotnet --list-runtimes 2>/dev/null | grep 'Microsoft.NETCore.App 9\.' >/dev/null; then
        warn "目标机需要 .NET 10 运行时（当前未检测到），或改用 SELF_CONTAINED=true 重新构建"
    fi
fi

# 桌面端在 Linux 上依赖 X11/Wayland 一整套系统库（这不是 NuGet 能带的），
# 缺了会在启动时报 "XOpenDisplay failed" 或 Avalonia 初始化异常。
if [ "$WITH_GUI" = "true" ]; then
    MISSING_X=""
    for so in libX11.so.6 libfontconfig.so.1 libICE.so.6 libSM.so.6; do
        # 不能用 grep -q：它匹配到就退出，ldconfig 会收 SIGPIPE，
        # 配合 pipefail 会把“有这个库”误判成失败
        if ! ldconfig -p 2>/dev/null | grep "$so" >/dev/null; then
            MISSING_X="$MISSING_X $so"
        fi
    done
    if [ -n "$MISSING_X" ]; then
        warn "本机缺少桌面运行库：$MISSING_X"
        warn "  目标机安装：sudo apt install libx11-6 libfontconfig1 libice6 libsm6"
    else
        ok "桌面运行库齐全（X11 / fontconfig）"
    fi
fi

# ---------- 2. 清理 ----------
if [ "$CLEAN" = "1" ]; then
    info "dotnet clean …"
    for p in "${PUBLISH_LIST[@]}"; do
        dotnet clean "$p/$p.csproj" -c "$CONFIG" -v quiet --nologo || warn "$p clean 未完全成功，继续发布"
    done
fi
clean_dir "$TMP_DIR"

# ---------- 3. 发布 ----------
# 两个项目必须各自发布到独立目录再合并：
# 直接往同一个 -o 里连发两次时，MSBuild 的 Copy 会因为目标目录里已有同名文件而
# 跳过覆盖，控制台那份 System.Text.Json/10.0.1 根本落不了地（实测产物里留下 9.0，
# 控制台启动即 FileNotFoundException）。改成显式 cp 合并，覆盖行为才是确定的。
PUBLISH_ARGS=(-c "$CONFIG" -r "$RID" --nologo)
if [ "$SELF_CONTAINED" = "true" ]; then
    PUBLISH_ARGS+=(--self-contained true)
else
    PUBLISH_ARGS+=(--self-contained false)
fi

CLI_STAGE=""
cleanup_stage() {
    if [ -n "${CLI_STAGE:-}" ] && [ -e "$CLI_STAGE" ]; then clean_dir "$CLI_STAGE"; fi
}
trap cleanup_stage EXIT

publish_project() {   # $1=项目名  $2=输出目录
    printf '\033[1;36m  → %s\033[0m\n' "$1"
    if ! dotnet publish "$1/$1.csproj" "${PUBLISH_ARGS[@]}" -o "$2"; then
        die "$1 发布失败，请检查上面的编译错误"
    fi
}

info "dotnet publish …（首次会下载 $RID 运行时包，可能稍慢）"
if [ "$WITH_GUI" = "true" ]; then
    # 桌面端先发到最终目录，作为基础产物
    publish_project "$GUI_PROJECT" "$TMP_DIR"

    # 控制台发到暂存目录，再整体覆盖过去：同名文件以控制台为准。
    # 冲突的只有 System.Text.Json / System.Text.Encodings.Web / System.IO.Pipelines
    # 三个（Console 走 Microsoft.Extensions 10.0.1 带的是 10.0.1，App 用的是框架自带 9.0），
    # 而这三者只被 Console 引用，App 的 deps.json 里没有，所以让 Console 胜出是安全的。
    CLI_STAGE="${TMP_DIR}-cli"
    clean_dir "$CLI_STAGE"
    publish_project "$CLI_PROJECT" "$CLI_STAGE"
    info "合并控制台产物（同名文件以控制台为准）"
    cp -a "$CLI_STAGE"/. "$TMP_DIR"/
    clean_dir "$CLI_STAGE"
    CLI_STAGE=""
else
    publish_project "$CLI_PROJECT" "$TMP_DIR"
fi

[ -f "$TMP_DIR/$CLI_BIN" ] || [ -f "$TMP_DIR/$CLI_BIN.dll" ] || die "发布目录里没有控制台可执行文件，产物异常"
if [ "$WITH_GUI" = "true" ]; then
    [ -f "$TMP_DIR/$GUI_BIN" ] || [ -f "$TMP_DIR/$GUI_BIN.dll" ] || die "发布目录里没有桌面端可执行文件，产物异常"
fi
ok "已生成：$TMP_DIR"

# ---------- 4. 原生库：先补 OpenCV，再补它缺的 ffmpeg 依赖 ----------
# 坑 1：OpenCvSharp 的运行时包只放了 runtimes/ubuntu.24.04-x64/native/libOpenCvSharpExtern.so，
#      没有 runtime.json 把它映射到 linux-x64。带 RID 发布时（--self-contained / -r linux-x64）
#      这个“发行版专用 RID”的资源不会被带上（NETSDK1206），产物里根本没有 OpenCV 原生库，
#      跑到 OCR 阶段才报 DllNotFoundException。这里从 NuGet 缓存手动补进输出目录。
EXTERN_SO="$TMP_DIR/libOpenCvSharpExtern.so"
if [ ! -f "$EXTERN_SO" ]; then
    NUGET_ROOT="${NUGET_PACKAGES:-}"
    if [ -z "$NUGET_ROOT" ]; then
        NUGET_ROOT="$(dotnet nuget locals global-packages --list 2>/dev/null \
            | sed 's/^[^:]*:[[:space:]]*//' | head -1)"
    fi
    if [ -n "$NUGET_ROOT" ] && [ -d "$NUGET_ROOT" ]; then
        CV_SO="$(find "$NUGET_ROOT" -path '*runtimes/ubuntu*/native/libOpenCvSharpExtern.so' \
            -print 2>/dev/null | sort -V | tail -1)"
        if [ -n "$CV_SO" ]; then
            cp -L "$CV_SO" "$EXTERN_SO"
            ok "已补入 OpenCV 原生库（来自 $CV_SO）"
        else
            warn "NuGet 缓存里找不到 libOpenCvSharpExtern.so（$NUGET_ROOT）"
            warn "先执行一次 dotnet restore 再重跑本脚本，否则运行到 OCR 会报 DllNotFoundException"
        fi
    fi
fi

# 坑 2：libOpenCvSharpExtern.so 是静态打包的 OpenCV，但仍动态依赖 ffmpeg 的几个库。
#      Ubuntu 26.04 只带 libswscale.so.9，而它要 libswscale.so.7。
#      把 ldd 报缺的库从本机已有副本拷进 <输出>/lib，由 run*.sh 注入 LD_LIBRARY_PATH。
#      依赖可能连锁（复制进来的库又缺别的库），所以循环解析几轮。
BUNDLED_LIBS=0
if [ -f "$EXTERN_SO" ] && command -v ldd >/dev/null 2>&1; then
    LIB_DIR="$TMP_DIR/lib"
    SEARCH_DIRS=(
        "$HOME/.local/lib/ffmpeg6"
        "$HOME/.local/lib"
        "/usr/lib/$(uname -m)-linux-gnu"
        "/usr/lib/x86_64-linux-gnu"
        "/usr/lib/aarch64-linux-gnu"
        "/usr/lib"
        "/lib/$(uname -m)-linux-gnu"
    )
    for _pass in 1 2 3; do
        # 已拷入的库本身也可能缺依赖，一并检查
        MISSING="$( { ldd "$EXTERN_SO" 2>/dev/null; \
                      for f in "$LIB_DIR"/*.so*; do [ -e "$f" ] && ldd "$f" 2>/dev/null; done; } \
                    | awk '/not found/ {print $1}' | sort -u || true)"
        [ -n "$MISSING" ] || break
        mkdir -p "$LIB_DIR"
        while IFS= read -r lib; do
            [ -n "$lib" ] || continue
            [ -e "$LIB_DIR/$lib" ] && continue
            src=""
            for d in "${SEARCH_DIRS[@]}"; do
                [ -e "$d/$lib" ] && { src="$d/$lib"; break; }
            done
            if [ -z "$src" ] && command -v ldconfig >/dev/null 2>&1; then
                src="$(ldconfig -p 2>/dev/null | awk -v n="$lib" '$1 == n {print $NF; exit}')"
            fi
            if [ -n "$src" ] && [ -e "$src" ]; then
                cp -L "$src" "$LIB_DIR/"
                BUNDLED_LIBS=$((BUNDLED_LIBS + 1))
                ok "已补齐原生库：$lib  ←  $src"
            else
                warn "缺少原生库 $lib，且本机没有可拷贝的副本"
                warn "  Ubuntu 26.04 上可执行：sudo apt install libswscale7（或取 noble 包 dpkg-deb -x 解包后重跑）"
            fi
        done <<< "$MISSING"
    done
fi
[ "$BUNDLED_LIBS" -gt 0 ] || ok "原生库依赖完整，无需额外补齐"

# ---------- 5. 精简：用空壳程序集替掉 V3/V4 的 OCR 模型包（省约 265MB）----------
# 背景：Sdcb.PaddleOCR.Models.Local（21MB，含 cls 模型和全部模型类）通过 Utils.LocalModel
# 按 ModelVersion 切换 typeof(KnownModels) 选程序集。即便只用 V5 模型，JIT 编译该方法体时
# 也要解析 V3/V4 的 KnownModels 类型，缺了就地报 FileNotFoundException。
# 而 V3/V4 两个包 245MB 全是本工具用不到的旧版模型，因此用同名同命名空间的
# 5KB 空壳替掉：类型解析照常通过，V5 分支的模型资源仍从 LocalV5 包里读。
STUB_SRC="tools/ocr-model-stubs"
if [ -f "$STUB_SRC/OcrModelStub.csproj" ] && [ -f "$TMP_DIR/Sdcb.PaddleOCR.Models.LocalV3.dll" ]; then
    info "精简 OCR 模型包（V3/V4 → 空壳）"
    STUB_ROOT="$(mktemp -d "${TMPDIR:-/tmp}/ia-ocr-stub.XXXXXX")"
    for v in 3 4; do
        STUB_WORK="$STUB_ROOT/v$v"
        mkdir -p "$STUB_WORK/src"
        # 拷一份源码到临时目录再编，避免两次编译共用 obj 目录互相污染
        cp "$STUB_SRC/OcrModelStub.csproj" "$STUB_SRC/KnownModels.cs" "$STUB_WORK/src/"
        if dotnet build "$STUB_WORK/src/OcrModelStub.csproj" -c Release --nologo -v quiet \
            -p:AssemblyName="Sdcb.PaddleOCR.Models.LocalV$v" \
            -p:DefineConstants="STUB_V$v" \
            -o "$STUB_WORK/out" >/dev/null 2>&1 \
           && [ -f "$STUB_WORK/out/Sdcb.PaddleOCR.Models.LocalV$v.dll" ]; then
            BEFORE=$(stat -c %s "$TMP_DIR/Sdcb.PaddleOCR.Models.LocalV$v.dll" 2>/dev/null || echo 0)
            cp -f "$STUB_WORK/out/Sdcb.PaddleOCR.Models.LocalV$v.dll" "$TMP_DIR/"
            ok "V$v 模型包 $((BEFORE / 1048576))MB → $(stat -c %s "$TMP_DIR/Sdcb.PaddleOCR.Models.LocalV$v.dll") 字节"
        else
            warn "V$v 空壳构建失败，保留原模型包（体积会大 250MB 左右）"
        fi
    done
    clean_dir "$STUB_ROOT"
fi

# ---------- 6. 精简：版本化 .so 去重（省约 65MB）----------
# NuGet 原生包会把 libopenvino.so / .so.2500 / .so.2025.0.0 这类软链关系在发布时摊成
# 三份完全相同的实体文件。按内容哈希保留一份，其余改成相对软链接。
DEDUP_COUNT=0
DEDUP_SAVED=0
declare -A _SEEN_HASH=()
while IFS= read -r f; do
    h="$(md5sum "$f" 2>/dev/null | awk '{print $1}')"
    [ -n "$h" ] || continue
    if [ -n "${_SEEN_HASH[$h]:-}" ]; then
        master="${_SEEN_HASH[$h]}"
        sz="$(stat -c %s "$f" 2>/dev/null || echo 0)"
        /bin/rm -f -- "$f"
        ln -s "$(basename "$master")" "$f"
        DEDUP_COUNT=$((DEDUP_COUNT + 1))
        DEDUP_SAVED=$((DEDUP_SAVED + sz))
    else
        _SEEN_HASH[$h]="$f"
    fi
done < <(find "$TMP_DIR" -maxdepth 1 -name '*.so*' -type f -size +1M | sort)
unset _SEEN_HASH
if [ "$DEDUP_COUNT" -gt 0 ]; then
    ok "去重 $DEDUP_COUNT 个重复 .so，省 $((DEDUP_SAVED / 1048576))MB（用软链接，打包请用 tar 以保留链接）"
fi

# ---------- 7. 精简：清掉 Windows 专用文件 ----------
# 注意 x64/ 目录里既有 Windows 的 *.dll，也有 Core.csproj 拷进来的 Linux 版
# libleptonica/libtesseract（.so），所以只删 dll，不能整目录删。
if [ -d "$TMP_DIR/x86" ]; then
    clean_dir "$TMP_DIR/x86"
    ok "已删除 x86/（Windows 32 位 Tesseract/Leptonica，Linux 用不到）"
fi
if [ -d "$TMP_DIR/x64" ]; then
    find "$TMP_DIR/x64" -maxdepth 1 -name '*.dll' -delete 2>/dev/null || true
    ok "已删除 x64/*.dll（Windows 版 Leptonica/Tesseract）"
fi

# ---------- 8. 生成启动脚本 ----------
if [ "$SELF_CONTAINED" = "true" ]; then
    RUNTIME_NOTE="自包含发布，目标机无需安装 .NET"
else
    RUNTIME_NOTE="框架依赖发布，目标机需安装 .NET 10 运行时"
fi

# $1=启动脚本名  $2=可执行文件名  $3=说明
make_launcher() {
    local script="$1" bin="$2" desc="$3" cmd
    if [ "$SELF_CONTAINED" = "true" ]; then
        cmd="exec \"./$bin\" \"\$@\""
    else
        cmd="exec dotnet \"./$bin.dll\" \"\$@\""
    fi
    cat > "$TMP_DIR/$script" <<EOF
#!/usr/bin/env bash
# 由 build-linux.sh 生成：注入原生库路径后启动$desc
set -euo pipefail
HERE="\$(cd "\$(dirname "\${BASH_SOURCE[0]}")" && pwd)"
if [ -d "\$HERE/lib" ]; then
    export LD_LIBRARY_PATH="\$HERE/lib\${LD_LIBRARY_PATH:+:\$LD_LIBRARY_PATH}"
fi
# 程序里有相对路径（config/process），必须切到自身目录再启动
cd "\$HERE"
$cmd
EOF
    chmod +x "$TMP_DIR/$script" 2>/dev/null || true
}

make_launcher "run.sh" "$CLI_BIN" "控制台"
ok "已生成 run.sh（控制台，$RUNTIME_NOTE）"
if [ "$WITH_GUI" = "true" ]; then
    make_launcher "run-gui.sh" "$GUI_BIN" "桌面端"
    ok "已生成 run-gui.sh（桌面端，$RUNTIME_NOTE）"
fi

# 发布目录里的可执行位也补一下（部分文件系统/拷贝方式会丢）
for b in "${PUBLISH_LIST[@]}"; do
    chmod +x "$TMP_DIR/$b" 2>/dev/null || true
done

# ---------- 9. 替换输出目录 ----------
clean_dir "$OUT_DIR"
mv "$TMP_DIR" "$OUT_DIR"

SIZE="$(du -sh "$OUT_DIR" 2>/dev/null | awk '{print $1}')"
COUNT="$(find "$OUT_DIR" -type f 2>/dev/null | wc -l)"
info "构建完成：$OUT_DIR  （$COUNT 个文件，$SIZE）"
cat <<EOF

下一步：
  1) 控制台：编辑 $OUT_DIR/appsettings.json，至少填好
       "RawFilesHome"              —— 要扫描的单据目录（绝对路径）
       "ProcessConfigurationHome"  —— 默认 config/process/aircas/，随产物已带
       "InvoiceOwner"              —— 报销人姓名
     然后运行： cd $OUT_DIR && ./run.sh

  2) 桌面端： cd $OUT_DIR && ./run-gui.sh
     界面里选单据目录、填报销人即可；配置文件同样在 config/process/ 下。
     纯 CLI 环境（无 X11/Wayland）只能跑控制台，桌面端启动会报显示相关错误。

  3) 两个入口都通过 run*.sh 启动：LD_LIBRARY_PATH 已由脚本处理，
     直接执行 ./$CLI_BIN / ./$GUI_BIN 可能因缺 libswscale 启动失败。

  4) 注意：程序会【移动并重命名】原目录里的单据，第一次跑请务必备份或用副本测试。

  5) 分发打包用 tar（有 6 个软链接，zip 默认不保留）：
       tar czf InvoiceAssistant-linux.tar.gz $OUT_DIR
EOF
