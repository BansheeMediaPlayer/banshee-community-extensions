#!/bin/bash

copyfiles ()
{
	echo "copying files"
	mkdir -p debian/usr/lib/banshee-1/Extensions
	mkdir -p debian/usr/share/doc/banshee-extension-streamrecorder
	mkdir -p debian/usr/share/locale/fi/LC_MESSAGES
	mkdir -p debian/usr/share/locale/en/LC_MESSAGES
	mkdir -p debian/usr/share/locale/de/LC_MESSAGES

	cp ../../bin/Banshee.Streamrecorder.dll debian/usr/lib/banshee-1/Extensions/
	#cp /usr/share/doc/banshee-extension-streamrecorder/AUTHORS debian/usr/share/doc/banshee-extension-streamrecorder/AUTHORS
	#cp /usr/share/doc/banshee-extension-streamrecorder/changelog.Debian.gz debian/usr/share/doc/banshee-extension-streamrecorder/changelog.Debian.gz
	#cp /usr/share/doc/banshee-extension-streamrecorder/copyright debian/usr/share/doc/banshee-extension-streamrecorder/copyright
	#cp /usr/share/doc/banshee-extension-streamrecorder/NEWS.gz debian/usr/share/doc/banshee-extension-streamrecorder/NEWS.gz
	#cp /usr/share/doc/banshee-extension-streamrecorder/README debian/usr/share/doc/banshee-extension-streamrecorder/README
	cp ../../po/de.gmo debian/usr/share/locale/de/LC_MESSAGES/banshee-community-extensions.mo
	cp ../../po/en.gmo debian/usr/share/locale/en/LC_MESSAGES/banshee-community-extensions.mo
	cp ../../po/fi.gmo debian/usr/share/locale/fi/LC_MESSAGES/banshee-community-extensions.mo
}

makedeb ()
{
	cd debian/usr/share/doc/banshee-extension-streamrecorder/
	gzip --best changelog.Debian
	gzip --best NEWS
	cd ../../../../..
	version="0.2.2beta-0ubuntu2"
	fakeroot dpkg-deb --build debian "banshee-extension-streamrecorder_"$version"_all.deb"
	lintian "banshee-extension-streamrecorder_"$version"_all.deb"
	cd debian/usr/share/doc/banshee-extension-streamrecorder/
	gunzip *.gz
	cd ../../../../..
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

