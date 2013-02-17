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
import java.util.ArrayList;

import android.content.Context;
import android.view.LayoutInflater;
import android.view.View;
import android.view.ViewGroup;
import android.widget.BaseAdapter;
import android.widget.TextView;

public class AlbumBaseAdapter extends BaseAdapter {
	 private static ArrayList<AlbumListItem> albumList;
	 
	 private LayoutInflater mInflater;

	 public AlbumBaseAdapter(Context context, ArrayList<AlbumListItem> albums) {
	  albumList = albums;
	  mInflater = LayoutInflater.from(context);
	 }

	 public int getCount() {
	  return albumList.size();
	 }

	 public Object getItem(int position) {
	  return albumList.get(position);
	 }

	 public long getItemId(int position) {
	  return position;
	 }

	 public View getView(int position, View convertView, ViewGroup parent) {
	  ViewHolder holder;
	  
	  //Log.i("banshee","Entered getView method");
	  if (convertView == null) {
		  //Log.i("banshee","convertView is null");
	   convertView = mInflater.inflate(R.layout.album_row_view, null);
	   holder = new ViewHolder();
	   holder.albumName = (TextView) convertView.findViewById(R.id.albumName);
	   holder.artist = (TextView) convertView.findViewById(R.id.albumArtist);

	   convertView.setTag(holder);
	  } else {
	   holder = (ViewHolder) convertView.getTag();
	  }
	  
	  holder.albumName.setText(albumList.get(position).getAlbumName());
	  holder.artist.setText(albumList.get(position).getArtist());

	  return convertView;
	 }

	 public class ViewHolder {
	  TextView albumName;
	  TextView artist;
	 }
}
