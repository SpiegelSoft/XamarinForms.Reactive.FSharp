#!/usr/bin/env bash
#
# For Xamarin Android or iOS, change the package name located in AndroidManifest.xml. 
# AN IMPORTANT THING: YOU NEED DECLARE NASA_API_KEY ENVIRONMENT VARIABLE IN APP CENTER BUILD CONFIGURATION.

if [ ! -n "$NASA_API_KEY" ]
then
    echo "You need define the PACKAGE_NAME variable in App Center"
    exit
fi

ANDROID_MANIFEST_FILE=$APPCENTER_SOURCE_DIRECTORY/XamarinForms.Reactive.Sample.Mars.Android/Properties/AndroidManifest.xml

if [ -e "$ANDROID_MANIFEST_FILE" ]
then
    echo "Updating NASA API key to $NASA_API_KEY in AndroidManifest.xml"
	echo Replacement script: 's/NASA_API_KEY/'$NASA_API_KEY'/'
    sed -i '' 's/NASA_API_KEY/'$NASA_API_KEY'/' $ANDROID_MANIFEST_FILE

    echo "File content:"
    cat $ANDROID_MANIFEST_FILE
fi

