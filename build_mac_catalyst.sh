#!/bin/bash

# 构建FrpGUI Mac Catalyst版本的自动化脚本
echo "=== 开始构建FrpGUI Mac Catalyst版本 ==="

# 设置项目相关变量
PROJECT_NAME="FrpGUI"
OUTPUT_DIR="./Publish"
MACOS_OUTPUT_DIR="$OUTPUT_DIR/client-maccatalyst"
BUILD_TEMP_DIR="./Build/maccatalyst"
APP_BUNDLE_NAME="FrpGUI.app"
FRP_MODULE_DIR="./frp"
FRP_BUILD_DIR="./frp/build"
EXTERNAL_FRP_DIR="$OUTPUT_DIR/frp"

# 初始化和编译frp子模块的函数
initialize_and_build_frp() {
    local target_dir="$1"
    local force_build="$2"
    
    # 检查目标目录是否存在，如果不存在则创建
    if [ ! -d "$target_dir" ]; then
        echo "创建目标目录: $target_dir"
        mkdir -p "$target_dir"
    fi
    
    # 检查是否需要构建frp
    if [ "$force_build" != "true" ] && [ -f "$target_dir/frpc" ] && [ -f "$target_dir/frps" ]; then
        echo "frp二进制文件已存在，跳过编译过程..."
        return 0
    fi
    
    echo "开始初始化和编译frp子模块..."
    
    # 检查Go是否安装
    if ! command -v go &> /dev/null; then
        echo "警告: 未安装Go，无法编译frp。尝试直接使用二进制文件。"
    else
        # 初始化和更新子模块
        echo "初始化和更新frp子模块..."
        git submodule init "$FRP_MODULE_DIR"
        git submodule update "$FRP_MODULE_DIR"
        
        # 编译frp for macOS
if [ -d "$FRP_MODULE_DIR" ]; then
    # 确保构建目录存在
    mkdir -p "$FRP_BUILD_DIR"
    
    # 进入frp目录并编译
    cd "$FRP_MODULE_DIR"
    
    echo "编译frp for macOS (当前架构)..."
    # 尝试使用make命令编译（如果存在Makefile）
    if [ -f "Makefile" ]; then
        echo "使用Makefile编译frp..."
        make build || echo "Makefile编译失败，尝试直接使用go build"
    fi
    
    # 先尝试编译当前架构的版本
    go build -o "../$FRP_BUILD_DIR/frpc" ./cmd/frpc
    go_build_c_result=$?
    go build -o "../$FRP_BUILD_DIR/frps" ./cmd/frps
    go_build_s_result=$?
    
    cd ..
    
    # 如果编译成功，复制到目标位置
    if [ $go_build_c_result -eq 0 ] && [ $go_build_s_result -eq 0 ]; then
        # 复制编译好的frp文件到应用包
        echo "复制编译好的frp文件..."
        cp -f "$FRP_BUILD_DIR/frpc" "$target_dir/frpc"
        cp -f "$FRP_BUILD_DIR/frps" "$target_dir/frps"
        
        # 设置可执行权限
        chmod +x "$target_dir/frpc"
        chmod +x "$target_dir/frps"
        
        echo "frp二进制文件已成功编译并复制到: $target_dir"
        return 0
    else
        echo "警告: frp编译失败，尝试寻找预编译的二进制文件"
    fi
else
    echo "警告: 未找到frp子模块目录"
fi
    fi
    
    # 尝试多种可能的预编译二进制文件位置
    local binary_locations=(
        "$FRP_MODULE_DIR/bin"
        "$FRP_MODULE_DIR/build"
        "./frp/bin"
        "./frp/build"
    )
    
    for location in "${binary_locations[@]}"; do
        if [ -f "$location/frpc" ] && [ -f "$location/frps" ]; then
            echo "找到预编译的frp二进制文件在: $location，复制中..."
            cp -f "$location/frpc" "$target_dir/frpc"
            cp -f "$location/frps" "$target_dir/frps"
            chmod +x "$target_dir/frpc"
            chmod +x "$target_dir/frps"
            echo "frp二进制文件已成功复制到: $target_dir"
            return 0
        fi
    done
    
    # 如果都找不到，提示手动下载
    echo "错误: 无法找到或编译frp二进制文件"
    echo "请手动下载frp二进制文件并放置到: $target_dir"
    echo "下载地址: https://github.com/fatedier/frp/releases"
    
    return 1
}

# 检查是否安装了.NET SDK
echo "检查.NET SDK安装情况..."
if ! command -v dotnet &> /dev/null; then
    echo "错误: 未安装.NET SDK，请先安装.NET 8.0或更高版本"
    exit 1
fi

# 检查是否在正确的项目目录
echo "检查项目文件..."
if [ ! -f "FrpGUI.sln" ]; then
    echo "错误: 未找到FrpGUI.sln文件，请确保在正确的项目目录中运行脚本"
    exit 1
fi

# 设置临时目录环境变量，解决macOS上构建路径问题
export Temp="$PWD/Temp"
mkdir -p "$Temp"
echo "设置临时目录: $Temp"

# 清理之前的构建产物
echo "清理构建目录..."
rm -rf "$MACOS_OUTPUT_DIR"
rm -rf "$BUILD_TEMP_DIR"
rm -rf "$Temp"
rm -rf "$EXTERNAL_FRP_DIR"
mkdir -p "$MACOS_OUTPUT_DIR"
mkdir -p "$BUILD_TEMP_DIR"
mkdir -p "$Temp"
mkdir -p "$EXTERNAL_FRP_DIR"

# 构建Mac Catalyst版本
echo "开始构建Mac Catalyst版本..."

# 首先构建基础项目
echo "构建解决方案..."
dotnet build FrpGUI.sln -c Release
if [ $? -ne 0 ]; then
    echo "错误: 解决方案构建失败"
    exit 1
fi

# 发布Avalonia.Desktop项目为macOS
# 注意：当前.NET和Avalonia对Mac Catalyst的支持是通过macOS目标实现的
echo "发布Avalonia.Desktop项目..."
dotnet publish FrpGUI.Avalonia.Desktop -c Release -r osx-x64 --self-contained true -o "$MACOS_OUTPUT_DIR" /p:PublishAot=true
if [ $? -ne 0 ]; then
    echo "警告: AOT构建失败，尝试不使用AOT..."
    dotnet publish FrpGUI.Avalonia.Desktop -c Release -r osx-x64 --self-contained true -o "$OUTPUT_DIR" /p:PublishAot=false
    if [ $? -ne 0 ]; then
        echo "错误: 项目发布失败"
        exit 1
    fi
fi

# 为Mac Catalyst创建应用包结构
echo "创建Mac Catalyst应用包结构..."
MACOS_BUNDLE_DIR="$MACOS_OUTPUT_DIR/$APP_BUNDLE_NAME/Contents/MacOS"
MACOS_RESOURCES_DIR="$MACOS_OUTPUT_DIR/$APP_BUNDLE_NAME/Contents/Resources"
mkdir -p "$MACOS_BUNDLE_DIR"
mkdir -p "$MACOS_RESOURCES_DIR"

# 复制必要的文件到应用包
echo "复制文件到应用包..."

# 首先确保目标目录存在
mkdir -p "$MACOS_BUNDLE_DIR"
mkdir -p "$MACOS_RESOURCES_DIR"

# 复制所有文件到MacOS目录，但排除应用包本身（避免递归复制）
# 先列出所有文件，排除应用包目录
find "$MACOS_OUTPUT_DIR" -maxdepth 1 -not -path "$MACOS_OUTPUT_DIR" -not -path "$MACOS_OUTPUT_DIR/$APP_BUNDLE_NAME" | while read file; do
    cp -r "$file" "$MACOS_BUNDLE_DIR/"
done

# 查找并确保主可执行文件存在
echo "检查主可执行文件..."
ls -la "$MACOS_OUTPUT_DIR" | grep -v "\." | grep -v "^d" | head -5

# 复制图标资源
if [ -d "$MACOS_OUTPUT_DIR/Assets" ]; then
    cp -r "$MACOS_OUTPUT_DIR/Assets"/* "$MACOS_RESOURCES_DIR/"
fi

# 设置正确的可执行权限
find "$MACOS_BUNDLE_DIR" -type f -exec chmod +x {} \; 2>/dev/null || true

# 创建Info.plist文件
echo "创建Info.plist文件..."
cat > "$MACOS_OUTPUT_DIR/FrpGUI.app/Contents/Info.plist" << EOL
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
    <key>CFBundleDevelopmentRegion</key>
    <string>zh_CN</string>
    <key>CFBundleExecutable</key>
    <string>$PROJECT_NAME</string>
    <key>CFBundleIdentifier</key>
    <string>com.frp.gui</string>
    <key>CFBundleInfoDictionaryVersion</key>
    <string>6.0</string>
    <key>CFBundleName</key>
    <string>$PROJECT_NAME</string>
    <key>CFBundlePackageType</key>
    <string>APPL</string>
    <key>CFBundleShortVersionString</key>
    <string>1.0</string>
    <key>CFBundleVersion</key>
    <string>1</string>
    <key>LSMinimumSystemVersion</key>
    <string>10.15</string>
    <key>NSHumanReadableCopyright</key>
    <string>© $(date +%Y) FrpGUI</string>
    <key>NSMainNibFile</key>
    <string></string>
    <key>NSPrincipalClass</key>
    <string>NSApplication</string>
    <key>LSApplicationCategoryType</key>
    <string>public.app-category.utilities</string>
</dict>
</plist>
EOL

# 处理frp子模块和二进制文件
echo "处理frp二进制文件..."

# 调用函数初始化和编译frp子模块
# 注意：这里设置为false，表示如果frp文件已存在则跳过编译
initialize_and_build_frp "$EXTERNAL_FRP_DIR" "false"

# 检查frp文件是否成功创建
if [ -f "$EXTERNAL_FRP_DIR/frpc" ] && [ -f "$EXTERNAL_FRP_DIR/frps" ]; then
    echo "frp文件已成功创建在外置目录: $EXTERNAL_FRP_DIR"
else
    echo "警告: frp文件准备失败，但将继续构建应用"
fi

# 设置可执行权限
echo "设置可执行权限..."
find "$OUTPUT_DIR/$APP_BUNDLE_NAME" -type f -name "*" -exec chmod +x {} \; 2>/dev/null || true

# 清理临时文件
echo "清理临时文件..."
# 删除应用包外的文件（保留应用包本身）
find "$MACOS_OUTPUT_DIR" -mindepth 1 -maxdepth 1 -not -name "$APP_BUNDLE_NAME" -exec rm -rf {} \; 2>/dev/null || true

# 完成构建
echo "=== Mac Catalyst版本构建完成 ==="
echo "应用文件位置: $MACOS_OUTPUT_DIR/$APP_BUNDLE_NAME"
echo "frp二进制文件位置: $EXTERNAL_FRP_DIR"
echo "提示: 可以双击应用包直接运行"
echo "注意: frp文件已放置在Publish目录下，完全独立于应用包，便于独立更新和管理"

echo "\n构建输出目录:"
ls -la "$OUTPUT_DIR"