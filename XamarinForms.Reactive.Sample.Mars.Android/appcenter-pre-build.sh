#!/usr/bin/env bash
#
# For Xamarin Android or iOS, change the package name located in AndroidManifest.xml and Info.plist. 
# AN IMPORTANT THING: YOU NEED DECLARE PACKAGE_NAME ENVIRONMENT VARIABLE IN APP CENTER BUILD CONFIGURATION.

if [ ! -n "$NASA_API_KEY" ]
then
    echo "You need define the PACKAGE_NAME variable in App Center"
    exit
fi

ANDROID_MANIFEST_FILE=$APPCENTER_SOURCE_DIRECTORY/Droid/Properties/AndroidManifest.xml

if [ -e "$ANDROID_MANIFEST_FILE" ]
then
    echo "Updating NASA API key to $NASA_API_KEY in AndroidManifest.xml"
    sed -i '' 's/meta-data android:name="nasa-api-key" android:value="[a-zA-Z0-9]*"/meta-data android:name="nasa-api-key" android:value="'$NASA_API_KEY'"/' $ANDROID_MANIFEST_FILE

    echo "File content:"
    cat $ANDROID_MANIFEST_FILE
fi

