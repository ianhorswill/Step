# remove apple quarantine attribute
xattr -d com.apple.quarantine *

# set executable bit
chmod +x ./StepRepl

# code sign it
# future note: --deep is deprecated, so you may need to specify different entitlements for the libraries
# see: https://forums.developer.apple.com/forums/thread/129980
codesign --force -s - ./StepRepl

# run that executable baby
./StepRepl