# remove apple quarantine attribute
xattr -d com.apple.quarantine *

# set executable bit
chmod +x ./StepRepl

# code sign it
codesign --force --deep -s - ./StepRepl

# run that executable baby
./StepRepl