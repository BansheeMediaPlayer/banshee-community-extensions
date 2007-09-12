#!/bin/bash

rm -Rf ~/.mirage
sqlite3 ~/.gnome2/banshee/banshee.db "delete from MirageProcessed;"
sqlite3 ~/.config/banshee/banshee.db "delete from MirageProcessed;"
