import os
import subprocess
import shutil

# List of target platforms
platforms = ["win-x64", "linux-x64", "osx-x64"]
current_platform = ""
netver = "net8.0"

def open_output_folder():
    if os.sys.platform == "darwin":
        os.system(f"open ./bin/Release/{netver}/{current_platform}")
    elif os.sys.platform == "win32":
        os.system("start " + os.path.join(os.getcwd(), f"bin\\Release\\{netver}\\{current_platform}"))

def create_app_bundle():
    print("Creating .app bundle...")
    # copy BuildAssets to the output folder
    shutil.copytree("BuildAssets/StepRepl.app", f"bin/Release/{netver}/{current_platform}/StepRepl.app", dirs_exist_ok=True)
    # copy contents of publish to StepRepl.app/Contents/MacOS
    shutil.copytree(f"bin/Release/{netver}/{current_platform}/publish", f"bin/Release/{netver}/{current_platform}/StepRepl.app/Contents/MacOS", dirs_exist_ok=True)

    if (os.sys.platform == "darwin"):
        print("Codesigning...")
    else:
        print("WARNING! This platform can't codesign app bundles and/or change the executable bit. Macs may complain or not run the app.")

def do_build():
    print(f"Publishing for {current_platform}...")
    command = f"dotnet publish StepRepl -r {current_platform} -p:IncludeNativeLibrariesForSelfExtract=true -p:PublishReadyToRunComposite=true -p:UseAppHost=true --self-contained true"
    # grab parent directory
    parent = os.path.dirname(os.getcwd())
    subprocess.run(command, shell=True, cwd=parent, check=True)
    print(f"Publish completed for {current_platform}.")
    open_output_folder()
    if current_platform.startswith("osx"):
        create_app_bundle()

if len(os.sys.argv) > 1:
    if os.sys.argv[1] == "--help":
        print("""
    Usage: python publish.py [options]
    Script to run `dotnet publish` for a variety of platforms.
    Mac builds are packaged into an .app bundle, the resources for which are in the BuildAssets folder.
    
    Options:
    --help: Show this help message.
    --platforms: List available platforms.
    --target <platform>: Publish for a specific platform. Use 'all' to publish for all platforms.
    open: Open the output folder in the file explorer.""")
        os.sys.exit(0)
    elif os.sys.argv[1] == "--platforms":
        print("Available platforms:")
        for platform in platforms:
            print(f"  {platform}")
        print("Use '--target <platform>' to publish for a specific platform. '--target all' publishes for all platforms.")
        os.sys.exit(0)
    elif os.sys.argv[1] == "--target" or os.sys.argv[1] == "-t" or os.sys.argv[1] == "-p" or os.sys.argv[1] == "--platform":
        if len(os.sys.argv) < 3:
            print("Error: No target platform specified.")
            os.sys.exit(1)
        target = os.sys.argv[2]
        if target not in platforms and target != "all":
            print(f"Error: Invalid target platform '{target}'. Use '--platforms' to list available platforms.")
            os.sys.exit(1)

        if target == "all":
            for platform in platforms:
                current_platform = platform
                do_build()
        else:
            current_platform = target
            do_build()

        os.sys.exit(0)
    elif os.sys.argv[1] == "open":
        open_output_folder()
        os.sys.exit(0)   