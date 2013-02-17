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

import java.io.File;
import java.util.ArrayList;

import android.app.Activity;
import android.content.Intent;
import android.database.Cursor;
import android.database.sqlite.SQLiteDatabase;
import android.database.sqlite.SQLiteException;
import android.os.Bundle;
import android.os.Environment;
import android.util.Log;
import android.view.View;
import android.widget.AdapterView;
import android.widget.ImageView;
import android.widget.ListView;
import android.widget.TextView;
import android.widget.Toast;
import android.widget.AdapterView.OnItemClickListener;

public class SongBrowse extends Activity{
	ArrayList<SongListItem> songList;
	//ArrayList<String> trackIdList;
    SongBaseAdapter songListAdapter; 
    final String filenameDB = "banshee.db";
    SQLiteDatabase bansheeDB;
    public int albumID;
    public ImageView icon;
    public TextView title;
    public Cursor c;
    public String[] params = null;
    public String query;
    
    protected void onActivityResult(int requestCode, int resultCode, Intent data){
		super.onActivityResult(requestCode, resultCode, data);
		finish();
	}
    
	public void onCreate(Bundle savedInstanceState) {
		//this.setFastScrollEnabled(true);
        super.onCreate(savedInstanceState);
        setContentView(R.layout.song_browse);
        Bundle extras = getIntent().getExtras();
        String titleText = extras.getString("Album");
        this.albumID = extras.getInt("AlbumID");
        this.icon = (ImageView)this.findViewById(R.id.icon);
        this.title = (TextView)this.findViewById(R.id.title);
        this.title.setText(titleText);
        this.icon.setImageResource(R.drawable.songs);
        this.songList = new ArrayList<SongListItem>();
        this.songListAdapter = new SongBaseAdapter(this,songList);
        //this.trackIdList = new ArrayList<Integer>();
        //this.songListAdapter = new ArrayAdapter<String>(this,R.layout.list_item,songList);
        //this.header = new ArrayList<String>();
        //this.header.add("title");
        //this.headerAdapter = new ArrayAdapter<String>(this,R.layout.list_item,header);
        //setListAdapter(headerAdapter);
        //setListAdapter(songListAdapter);
        final ListView l = (ListView) findViewById(R.id.songBrowse);
        l.setAdapter(songListAdapter);
		l.setTextFilterEnabled(true);
		l.setFastScrollEnabled(true);
        File db = null;
		try{ 
			//db = getFileStreamPath(filenameDB);
			db = Environment.getExternalStorageDirectory();
			String path = db.getAbsolutePath()+'/'+filenameDB;
			bansheeDB = SQLiteDatabase.openDatabase(path, null, SQLiteDatabase.NO_LOCALIZED_COLLATORS);
			query = "SELECT CoreTracks.Title AS trackTitle, CoreTracks.Uri,  CoreArtists.Name, CoreAlbums.Title FROM CoreTracks,CoreAlbums,CoreArtists WHERE CoreTracks.AlbumID==CoreAlbums.AlbumID AND CoreTracks.ArtistID==CoreArtists.ArtistID";
			
			if(albumID>=0){
				query+=" AND CoreTracks.AlbumID=?";
				params = new String[]{String.valueOf(albumID)};
				query+=" ORDER BY CoreTracks.TrackNumber";
			}
			else{
				query+=" ORDER BY CoreTracks.Title";
			}
			//Thread queryDB = new Thread(new Runnable(){
				//public void run(){
					c = bansheeDB.rawQuery(query,params);
					int name = c.getColumnIndex("trackTitle");
					int artist = c.getColumnIndex("CoreArtists.Name");
					int albumColumnIndex = c.getColumnIndex("CoreAlbums.Title");
					int uriColumnIndex = c.getColumnIndex("CoreTracks.Uri");
					Log.i("banshee","coretrack title index "+name+" corealbum title index "+albumColumnIndex+" number of columns "+c.getColumnCount());
					for(int i=0;i<c.getColumnCount();i++){
						Log.i("banshee",c.getColumnName(i));
					}
					//int albumColumnIndex = c.getColumnIndex("CoreAlbums.Title");
					//int trackIdColumn = c.getColumnIndex("TrackID");
					//int count = c.getCount();
					c.moveToFirst();
					while(c.isAfterLast()==false){
						SongListItem songItem = new SongListItem();
						String songName = c.getString(name);
						String artistName = c.getString(artist);
						String albumName = c.getString(albumColumnIndex);
						String uri = c.getString(uriColumnIndex);
						String combination = "";
						songItem.setSongName(songName);
						if(artistName!=null){
							combination+=artistName;
							if(albumName!=null)
								combination+='/'+albumName;
						}	
						else{
							combination+="Unknown Artist";
							if(albumName!=null)
								combination+='/'+albumName;
						}
						songItem.setUri(uri);
						songItem.setArtist(combination);
						//String trackID = c.getString(trackIdColumn);
						if(songName!=null){
							songList.add(songItem);
							//trackIdList.add(c.getString(trackIdColumn));
						}
						c.moveToNext();
						//artistList.add("hello");
					}
					c.close();
				//}
			//});
			//queryDB.start();
			//int columnCount = c.getColumnCount();
			
			//artistList.add("hello");
			//Toast.makeText(this,result,Toast.LENGTH_SHORT).show();
			//String result = c.getString(name);
		}catch(SQLiteException e){
			Toast.makeText(this,e.getMessage(),Toast.LENGTH_SHORT).show();
			//Toast.makeText(this,"Something went wrong. Make sure the banshee database file is on your sd-card.",Toast.LENGTH_LONG).show();
		}
		catch(Exception e){
			Toast.makeText(this,e.getMessage(),Toast.LENGTH_SHORT).show();
			//Toast.makeText(this,"Something went wrong. Make sure the banshee database file is on your sd-card.",Toast.LENGTH_LONG).show();
		}
		
		l.setOnItemClickListener(new OnItemClickListener(){
	         public void onItemClick(AdapterView<?> a, View v, int position, long id) { 
	          Object o = l.getItemAtPosition(position);
	          SongListItem song = (SongListItem)o;
	          Intent response = new Intent();
	      	  response.putExtra("Uri",song.getUri());
	      	  setResult(RESULT_OK,response);
	      	  finish();
	          }  
		});
    }
	/*public boolean onKeyDown(int keyCode, KeyEvent event) {
	    if ((keyCode == KeyEvent.KEYCODE_BACK)) {
	        //Log.d(this.getClass().getName(), "back button pressed");
	    	Intent response = new Intent();
	    	setResult(RESULT_OK+1,response);
	    	finish();
	    }
	    return super.onKeyDown(keyCode, event);
	}*/
	
	/*public void onListItemClick(ListView l, View v, int position, long id) {
    	super.onListItemClick(l, v, position, id);
    	String selected = this.songListAdapter.getItem(position);
    	//String trackID = this.trackIdList.get(position);
    	//Toast.makeText(this,selected,Toast.LENGTH_SHORT).show();
    	query = "SELECT Uri FROM CoreTracks WHERE Title=?";
		params = new String[]{selected};	
		if(albumID>=0){
			query+=" AND AlbumID=?";
			params = new String[]{selected, String.valueOf(albumID)};
			query+=" ORDER BY TrackNumber";
		}
		else{
			query+=" ORDER BY Title";
		}
		this.c = bansheeDB.rawQuery(query,params);
		//int columnCount = c.getColumnCount();
		int uriColumn = c.getColumnIndex("Uri");
		//int count = c.getCount();
		c.moveToFirst();
		String Uri = c.getString(uriColumn);
		//Toast.makeText(this,Uri,Toast.LENGTH_SHORT).show();
    	Intent response = new Intent();
    	response.putExtra("Uri",Uri);
    	setResult(RESULT_OK,response);
    	finish();
    	//startActivityForResult(i,0);
	}*/

}
