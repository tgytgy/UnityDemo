#!/usr/bin/env python3
# -*- coding: utf-8 -*-

"""
生成 Protobuf 代码（Go + C#，长度前缀/协议无关）
- 在 Go 项目根目录执行 go 输出（--go_out=.）
- 将 C# 输出写到 Unity 工程的绝对路径（--csharp_out=<abs>）

默认值可在下方常量处配置，也可通过命令行参数覆盖。
"""

import argparse
import os
import shutil
import subprocess
import sys
from pathlib import Path

# ========= 默认配置（可改） =========
# 你的 Go 项目根目录（绝对路径）
PROJECT_ROOT_DEFAULT = r"C:\\Tgy\\Github\\UnityDemo\\GoServer\\GoGameServer"  # 例：r"C:\Users\you\go\tcp-proto-demo" 或 "/Users/you/go/tcp-proto-demo"

# 相对 PROJECT_ROOT 的 proto 文件路径（也可用绝对路径覆盖）
PROTO_FILE_DEFAULT = "proto/echo.proto"

# 你的 Unity 工程里用于存放生成 C# 的**绝对路径**
UNITY_OUT_DEFAULT = r"C:\\Users\\17483\\MyProject\\Assets\\Core\\Proto"  # 例："/Users/you/Unity/MyGame/Assets/Scripts/Proto"

# ===================================


def which_or_hint(cmd_name: str) -> bool:
    """检查命令是否在 PATH 中。"""
    return shutil.which(cmd_name) is not None


def run_cmd(cmd, cwd=None):
    cwd_disp = str(cwd) if cwd else os.getcwd()
    print(f"\n>>> 执行: {' '.join(cmd)}")
    print(f"    工作目录: {cwd_disp}")
    try:
        # 指定 universal_newlines=True 和 encoding='utf-8'，防止 GBK 解码出错
        result = subprocess.run(
            cmd,
            stdout=subprocess.PIPE,
            stderr=subprocess.STDOUT,
            text=True,
            encoding="utf-8",  # 强制用 UTF-8 解析输出
            cwd=cwd,
            check=False,
        )
    except FileNotFoundError as e:
        print(f"❌ 无法执行命令（可能未安装或不在 PATH）: {e}")
        sys.exit(1)

    # 如果还是有无法解码的字符，替换掉不支持的部分
    print(result.stdout)
    if result.returncode != 0:
        print("❌ 命令执行失败，请检查上面的错误信息。")
        sys.exit(result.returncode)



def resolve_path(p: str, base: Path | None = None) -> Path:
    """将路径转为绝对路径；若是相对路径，则基于 base（若 base 为空则基于当前目录）"""
    path = Path(p)
    if not path.is_absolute():
        if base is None:
            base = Path.cwd()
        path = (base / path).resolve()
    return path


def main():
    parser = argparse.ArgumentParser(description="生成 Protobuf 代码（Go + C#）")
    parser.add_argument("--project-root", dest="project_root", default=PROJECT_ROOT_DEFAULT,
                        help="Go 项目根目录（绝对路径），默认为脚本顶部的 PROJECT_ROOT_DEFAULT")
    parser.add_argument("--proto", dest="proto_file", default=PROTO_FILE_DEFAULT,
                        help="proto 文件路径，默认相对 project-root，也可传绝对路径")
    parser.add_argument("--unity-out", dest="unity_out", default=UNITY_OUT_DEFAULT,
                        help="C# 输出目录（绝对路径），默认为脚本顶部的 UNITY_OUT_DEFAULT")
    parser.add_argument("--skip-go", action="store_true", help="仅生成 C#，跳过 Go 代码生成")
    parser.add_argument("--skip-cs", action="store_true", help="仅生成 Go，跳过 C# 代码生成")
    args = parser.parse_args()

    project_root = resolve_path(args.project_root)
    proto_path = resolve_path(args.proto_file, base=project_root)
    unity_out = resolve_path(args.unity_out)

    print("==== 配置 ====")
    print(f"Go 项目根目录: {project_root}")
    print(f"Proto 文件   : {proto_path}")
    print(f"C# 输出目录  : {unity_out}")
    print("================")

    # 基础存在性检查
    if not project_root.exists():
        print(f"项目根目录不存在: {project_root}")
        sys.exit(1)
    if not proto_path.exists():
        print(f"找不到 proto 文件: {proto_path}")
        sys.exit(1)

    # 工具检查
    if not which_or_hint("protoc"):
        print("未找到 'protoc'，请先安装并加入 PATH。")
        print("   参考: https://github.com/protocolbuffers/protobuf/releases")
        sys.exit(1)

    if not args.skip_go and not which_or_hint("protoc-gen-go"):
        print("未找到 'protoc-gen-go'，请安装并加入 PATH：")
        print("   go install google.golang.org/protobuf/cmd/protoc-gen-go@latest")
        print("   （确保你的 GOPATH/bin 在系统 PATH 中）")
        sys.exit(1)

    # 生成 Go 代码
    if not args.skip_go:
        print("\n=== 生成 Go 代码 ===")
        # 在 Go 项目根目录执行，确保 --go_out=. 输出到项目内
        run_cmd(
            ["protoc", "--go_out=.", "--go_opt=paths=source_relative", str(proto_path.relative_to(project_root))],
            cwd=project_root
        )
        print("Go 代码生成完成。")

    # 生成 C# 代码
    if not args.skip_cs:
        print("\n=== 生成 C# 代码（Unity）===")
        unity_out.mkdir(parents=True, exist_ok=True)
        # --csharp_out 支持绝对路径，这里直接传绝对路径
        # 注意：工作目录仍使用 project_root，以便 proto 的相对路径一致
        run_cmd(
            ["protoc", f"--csharp_out={str(unity_out)}", str(proto_path.relative_to(project_root))],
            cwd=project_root
        )
        print(f"C# 代码生成完成：{unity_out}")

    print("\n全部完成！")


if __name__ == "__main__":
    main()
