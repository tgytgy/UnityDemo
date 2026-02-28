import os
import sys


def main():
    script_dir = os.path.dirname(os.path.abspath(__file__))
    parent_dir = os.path.dirname(script_dir)

    core_path = os.path.join(parent_dir, "Core")
    if not os.path.isdir(core_path):
        print(f"错误：未在 {parent_dir} 中找到 Core 文件夹")
        sys.exit(1)

    print(f"找到 Core 文件夹：{core_path}")

    grandparent_dir = os.path.dirname(parent_dir)
    core_parent_name = os.path.basename(parent_dir)

    print(f"扫描目录：{grandparent_dir}")
    print(f"排除目录：{core_parent_name}")

    for entry in os.listdir(grandparent_dir):
        entry_path = os.path.join(grandparent_dir, entry)
        if not os.path.isdir(entry_path) or entry == core_parent_name:
            continue

        asset_path = os.path.join(entry_path, "Assets")
        if not os.path.isdir(asset_path):
            print(f"跳过 {entry}：未找到 Asset 文件夹")
            continue

        print(f"\n处理 {entry}/Asset ...")

        link_path = os.path.join(asset_path, "Core")
        if os.path.exists(link_path):
            if os.path.islink(link_path):
                print(f"  Core 软链接已存在：{link_path}")
            else:
                print(f"  警告：{link_path} 已存在但不是软链接，跳过")
        else:
            os.symlink(core_path, link_path, target_is_directory=True)
            print(f"  已创建软链接：{link_path} -> {core_path}")

        gameplay_path = os.path.join(asset_path, "GamePlay")
        if os.path.isdir(gameplay_path):
            print(f"  GamePlay 文件夹已存在")
        else:
            os.makedirs(os.path.join(gameplay_path, "Res"), exist_ok=True)
            os.makedirs(os.path.join(gameplay_path, "Src"), exist_ok=True)
            print(f"  已创建 GamePlay、GamePlay/Res、GamePlay/Src")

    print("\n完成。")


if __name__ == "__main__":
    main()
