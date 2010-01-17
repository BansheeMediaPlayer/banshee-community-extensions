#!/bin/bash

mkdir -p debian/usr/lib/banshee-1/Extensions
mkdir -p debian/usr/share/doc/banshee-extension-streamrecorder
mkdir -p debian/usr/share/locale/fi/LC_MESSAGES
mkdir -p debian/usr/share/locale/en/LC_MESSAGES
mkdir -p debian/usr/share/locale/de/LC_MESSAGES

cp /usr/lib/banshee-1/Extensions/Banshee.Streamrecorder.dll debian/usr/lib/banshee-1/Extensions/Banshee.Streamrecorder.dll
cp /usr/share/doc/banshee-extension-streamrecorder/AUTHORS debian/usr/share/doc/banshee-extension-streamrecorder/AUTHORS
cp /usr/share/doc/banshee-extension-streamrecorder/changelog.Debian.gz debian/usr/share/doc/banshee-extension-streamrecorder/changelog.Debian.gz
cp /usr/share/doc/banshee-extension-streamrecorder/copyright debian/usr/share/doc/banshee-extension-streamrecorder/copyright
cp /usr/share/doc/banshee-extension-streamrecorder/NEWS.gz debian/usr/share/doc/banshee-extension-streamrecorder/NEWS.gz
cp /usr/share/doc/banshee-extension-streamrecorder/README debian/usr/share/doc/banshee-extension-streamrecorder/README
cp /usr/share/locale/de/LC_MESSAGES/banshee-streamrecorder.mo debian/usr/share/locale/de/LC_MESSAGES/banshee-streamrecorder.mo
cp /usr/share/locale/en/LC_MESSAGES/banshee-streamrecorder.mo debian/usr/share/locale/en/LC_MESSAGES/banshee-streamrecorder.mo
cp /usr/share/locale/fi/LC_MESSAGES/banshee-streamrecorder.mo debian/usr/share/locale/fi/LC_MESSAGES/banshee-streamrecorder.mo

