#!/bin/bash

copyfiles ()
{
	echo "copying files"
	mkdir -p debian/usr/lib/banshee-1/Extensions
	mkdir -p debian/usr/share/doc/banshee-extension-streamrecorder
	mkdir -p debian/usr/share/locale/fi/LC_MESSAGES
	mkdir -p debian/usr/share/locale/en/LC_MESSAGES
	mkdir -p debian/usr/share/locale/de/LC_MESSAGES

	cp Banshee.Streamrecorder/bin/Release/Banshee.Streamrecorder.dll Banshee.Streamrecorder.dll debian/usr/lib/banshee-1/Extensions/Banshee.Streamrecorder.dll
	#cp /usr/share/doc/banshee-extension-streamrecorder/AUTHORS debian/usr/share/doc/banshee-extension-streamrecorder/AUTHORS
	#cp /usr/share/doc/banshee-extension-streamrecorder/changelog.Debian.gz debian/usr/share/doc/banshee-extension-streamrecorder/changelog.Debian.gz
	#cp /usr/share/doc/banshee-extension-streamrecorder/copyright debian/usr/share/doc/banshee-extension-streamrecorder/copyright
	#cp /usr/share/doc/banshee-extension-streamrecorder/NEWS.gz debian/usr/share/doc/banshee-extension-streamrecorder/NEWS.gz
	#cp /usr/share/doc/banshee-extension-streamrecorder/README debian/usr/share/doc/banshee-extension-streamrecorder/README
	cp po/de.gmo debian/usr/share/locale/de/LC_MESSAGES/banshee-streamrecorder.mo
	cp po/en.gmo debian/usr/share/locale/en/LC_MESSAGES/banshee-streamrecorder.mo
	cp po/fi.gmo debian/usr/share/locale/fi/LC_MESSAGES/banshee-streamrecorder.mo
}

makedeb ()
{
	fakeroot dpkg-deb --build debian
	mv debian.deb banshee-extension-streamrecorder_0.2.0-1ubuntu_all.deb
	lintian banshee-extension-streamrecorder_0.2.0-1ubuntu_all.deb 
}

printusage ()
{
	echo "usage: $0 [--build] [--copyfiles]"
	exit 0
}

if [ $# -lt 1 ]
then
	printusage
fi

for arg in $@
do
	case $arg in
	--build) 	makedeb ;;
	--copyfiles)	copyfiles ;;
	*) 		printusage ;;
	esac
done

