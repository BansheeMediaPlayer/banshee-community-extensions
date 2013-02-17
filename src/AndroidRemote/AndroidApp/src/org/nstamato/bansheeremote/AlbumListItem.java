package org.nstamato.bansheeremote;
/*
BansheeRemote

Copyright (C) 2011 Nikitas Stamatopoulos

Permission is hereby granted, free of charge, to any person obtaining
a copy of this software and associated documentation files (the
"Software"), to deal in the Software without restriction, including
without limitation the rights to use, copy, modify, merge, publish,
distribute, sublicense, and/or sell copies of the Software, and to
permit persons to whom the Software is furnished to do so, subject to
the following conditions:

The above copyright notice and this permission notice shall be
included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/
public class AlbumListItem {
	private String albumName = "";
	 private String artist = "";
	 private int albumId=0;

	 public void setAlbumName(String name) {
	  this.albumName = name;
	 }

	 public String getAlbumName() {
	  return albumName;
	 }
	 

	 public void setArtist(String artist) {
	  this.artist = artist;
	 }

	 public String getArtist() {
	  return artist;
	 }
	 
	 public void setAlbumId(int albumId){
		 this.albumId = albumId;
	 }
	 
	 public int getAlbumId(){
		 return albumId;
	 }
}
