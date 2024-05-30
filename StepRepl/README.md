

## Building and deploying

The `publish.py` script will call `dotnet publish` to build executables for distributing to users. You can run `py[thon] publish.py --help` for more information.

Feel free to edit that script to change build targets, parameters, and other procedures. Output binaries can be located by running `py[thon] publish.py open` or going to `/bin/Release/{platform}/publish`. The `publish` folder contains everything needed to distribute for Windows and Linux, and Mac applications are located in the platform's folder.

### Mac distribution process

Mac applications are actually folders with a specific structure. We have a template application in `BuildAssets` called `StepRepl.app` which already has relevant configuration. When `publish.py` is run, it will copy the build output into the `StepRepl.app/Contents/MacOS` folder.

When users run the application, it will execute the `prep_for_mac.sh` script which does ad-hoc code signing, adjusting file attributes, and then launches the application. This is sketchy as hell but so far it's been working fine.

StepRepl can compile binaries for Apple Silicon and x86, but for ease of distribution we only build and send the x86 version. Apple Silicon macs can still run it without issue using Rosetta. Future maintainers can look into making a Universal binary or transitioning to ARM fully.

For more information and troubleshooting, see the [Avalonia docs](https://docs.avaloniaui.net/docs/deployment/macOS).