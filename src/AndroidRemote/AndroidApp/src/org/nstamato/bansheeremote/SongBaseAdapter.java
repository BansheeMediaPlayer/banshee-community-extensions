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

public class SongBaseAdapter extends BaseAdapter {
	 private static ArrayList<SongListItem> songList;
	 
	 private LayoutInflater mInflater;

	 public SongBaseAdapter(Context context, ArrayList<SongListItem> songs) {
	  songList = songs;
	  mInflater = LayoutInflater.from(context);
	 }

	 public int getCount() {
	  return songList.size();
	 }

	 public Object getItem(int position) {
	  return songList.get(position);
	 }

	 public long getItemId(int position) {
	  return position;
	 }

	 public View getView(int position, View convertView, ViewGroup parent) {
	  ViewHolder holder;
	  
	  //Log.i("banshee","Entered getView method");
	  if (convertView == null) {
		  //Log.i("banshee","convertView is null");
	   convertView = mInflater.inflate(R.layout.song_row_view, null);
	   holder = new ViewHolder();
	   holder.songName = (TextView) convertView.findViewById(R.id.songName);
	   holder.artist = (TextView) convertView.findViewById(R.id.songArtist);

	   convertView.setTag(holder);
	  } else {
	   holder = (ViewHolder) convertView.getTag();
	  }
	  
	  holder.songName.setText(songList.get(position).getSongName());
	  holder.artist.setText(songList.get(position).getArtist());

	  return convertView;
	 }

	 public class ViewHolder {
	  TextView songName;
	  TextView artist;
	 }
}
