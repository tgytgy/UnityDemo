import os
import sys
import subprocess
import platform

def create_symlink(source, target):
    """
    创建软链接
    :param source: 源文件夹路径
    :param target: 目标文件夹路径
    """
    if not os.path.exists(source):
        print(f"源路径 '{source}' 不存在，跳过...")
        return

    if os.path.exists(target):
        print(f"目标路径 '{target}' 已存在，跳过...")
        return

    if platform.system() == "Windows":
        # Windows 使用 mklink 命令
        try:
            subprocess.run(["mklink", "/D", target, source], check=True, shell=True)
            print(f"成功创建软链接: {source} -> {target}")
        except subprocess.CalledProcessError as e:
            print(f"创建软链接失败: {e}")
    else:
        # Mac/Linux 使用 ln -s 命令
        try:
            os.symlink(source, target)
            print(f"成功创建软链接: {source} -> {target}")
        except OSError as e:
            print(f"创建软链接失败: {e}")

def main():
    # 获取脚本所在目录的上一级路径
    script_dir = os.path.dirname(os.path.abspath(__file__))
    parent_dir = os.path.dirname(script_dir)
    
    # 定义源文件夹和目标文件夹的映射关系（相对于父目录）
    symlink_map = {
        os.path.join(parent_dir, "Project", "Assets", "Core"): os.path.join(parent_dir, "Core"),
        os.path.join(parent_dir, "Project", "Assets", "ExternalRes"): os.path.join(parent_dir, "ExternalRes"),
        os.path.join(parent_dir, "Project", "Assets", "Scenes"): os.path.join(parent_dir, "Scenes")
        # 添加更多映射
    }

    # 检查是否是管理员权限（Windows 需要管理员权限创建软链接）
    if platform.system() == "Windows":
        try:
            import ctypes
            if not ctypes.windll.shell32.IsUserAnAdmin():
                print("在 Windows 上创建软链接需要管理员权限，请以管理员身份运行脚本。")
                sys.exit(1)
        except:
            print("无法检查Windows管理员权限，请确保以管理员身份运行脚本。")
            sys.exit(1)

    # 创建软链接
    for target, source in symlink_map.items():
        create_symlink(source, target)

    print("软链接创建完成。")

if __name__ == "__main__":
    main()