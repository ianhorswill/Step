import os
import subprocess

# List of target platforms
platforms = ["win-x64", "linux-x64", "osx-arm64", "osx-x64"]

for platform in platforms:
    print(f"Publishing for {platform}...")
    command = f"dotnet publish AvaloniaRepl -r {platform} -p:PublishSingleFile=true --self-contained true"
    # grab parent directory
    parent = os.path.dirname(os.getcwd())
    subprocess.run(command, shell=True, cwd=parent, check=True)
    

print("Publish completed for all platforms.")