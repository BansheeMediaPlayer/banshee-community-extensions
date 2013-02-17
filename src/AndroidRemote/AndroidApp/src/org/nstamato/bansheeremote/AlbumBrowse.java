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
import android.view.View;
import android.widget.AdapterView;
import android.widget.ImageView;
import android.widget.ListView;
import android.widget.TextView;
import android.widget.Toast;
import android.widget.AdapterView.OnItemClickListener;

public class AlbumBrowse extends Activity{
	ArrayList<AlbumListItem> albumList;
    AlbumBaseAdapter albumListAdapter; 
	//ArrayAdapter<AlbumListItem> albumListAdapter;
    final String filenameDB = "banshee.db";
    SQLiteDatabase bansheeDB;
    public ImageView icon;
    public TextView title;
    public String titleText="Albums";
    public boolean addedNull=false;
    //public ListView l;
    
    /*private OnItemClickListener clickListener = new OnItemClickListener(){
        public void onItemClick(AdapterView<?> a, View v, int position, long id) { 
	          Object o = l.getItemAtPosition(position);
	          AlbumListItem album = (AlbumListItem)o;
	          Intent i = new Intent(AlbumBrowse.this,SongBrowse.class);
	      		i.putExtra("Album",album.getAlbumName());
	      		i.putExtra("AlbumID",album.getAlbumId());
	      		startActivityForResult(i,2);
	          }  
		};*/
    
    protected void onActivityResult(int requestCode, int resultCode, Intent data){
		super.onActivityResult(requestCode, resultCode, data);
		setResult(resultCode,data);
		if(resultCode==RESULT_OK)
			finish();
	}
    
	public void onCreate(Bundle savedInstanceState) {
		//this.setFastScrollEnabled(true);
        super.onCreate(savedInstanceState);
        setContentView(R.layout.album_browse);
        Intent i=getIntent();
        Bundle extras=null;
        if(i!=null)
        	extras = i.getExtras();
        if(extras!=null)
        	this.titleText = extras.getString("Artist");
        this.icon = (ImageView)this.findViewById(R.id.icon);
        this.title = (TextView)this.findViewById(R.id.title);
        this.title.setText(titleText);
        this.icon.setImageResource(R.drawable.album);
        this.albumList = new ArrayList<AlbumListItem>();
        this.albumListAdapter = new AlbumBaseAdapter(this,albumList);
        //this.albumListAdapter = new ArrayAdapter<AlbumListItem>(this,R.layout.album_row_view,albumList);
        
        //this.header = new ArrayList<String>();
        //this.header.add("title");
        //this.headerAdapter = new ArrayAdapter<String>(this,R.layout.list_item,header);
        //setListAdapter(headerAdapter);
        
        //setListAdapter(albumListAdapter);
        ListView l = (ListView) findViewById(R.id.albumBrowse);
        l.setAdapter(albumListAdapter);
        //ListView l = getListView();
		l.setTextFilterEnabled(true);
		l.setFastScrollEnabled(true);
		//l.setTextFilterEnabled(true);
		//l.setFastScrollEnabled(true);
		//l.setOnItemClickListener(clickListener);
        File db = null;
		try{
			//db = getFileStreamPath(filenameDB);
			db = Environment.getExternalStorageDirectory();
			String path = db.getAbsolutePath()+'/'+filenameDB;
			bansheeDB = SQLiteDatabase.openDatabase(path, null, SQLiteDatabase.NO_LOCALIZED_COLLATORS);
			String query = "SELECT Title, TitleLowered, ArtistName, AlbumID FROM CoreAlbums";
			String[] params = null;
			if(!titleText.equals("Albums")){
				query+=" WHERE ArtistName=?";
				params = new String[]{titleText};
			}
			query+=" ORDER BY Title";
			
			Cursor c = bansheeDB.rawQuery(query,params);
			//int columnCount = c.getColumnCount();
			int name = c.getColumnIndex("Title");
			int lowered = c.getColumnIndex("TitleLowered");
			int artistColumnIndex = c.getColumnIndex("ArtistName");
			int albumIdColumnIndex = c.getColumnIndex("AlbumID");
			//int count = c.getCount();
			c.moveToFirst();
			while(c.isAfterLast()==false){
				AlbumListItem albumItem = new AlbumListItem();
				String label = c.getString(name);
				String artist = c.getString(artistColumnIndex);
				int albumID = c.getInt(albumIdColumnIndex);
				if(label!=null){
					albumItem.setAlbumName(label);
					albumItem.setAlbumId(albumID);
					albumItem.setArtist("");
					if(artist!=null)
						albumItem.setArtist(artist);
					albumList.add(albumItem);
				}
				else if(!addedNull){
					albumItem.setAlbumName(c.getString(lowered));
					albumItem.setAlbumId(albumID);
					albumItem.setArtist(("Unknown Artist"));
					albumList.add(albumItem);
					addedNull=true;
				}
				c.moveToNext();
				//artistList.add("hello");
			}
			c.close();
			//artistList.add("hello");
			//Toast.makeText(this,result,Toast.LENGTH_SHORT).show();
			//String result = c.getString(name);
		}catch(SQLiteException e){
			//Toast.makeText(this,e.getMessage(),Toast.LENGTH_SHORT).show();
			Toast.makeText(this,"Something went wrong. Make sure the banshee database file is on your sd-card.",Toast.LENGTH_LONG).show();
		}
		catch(Exception e){
			//Toast.makeText(this,e.getMessage(),Toast.LENGTH_SHORT).show();
			Toast.makeText(this,"Something went wrong. Make sure the banshee database file is on your sd-card.",Toast.LENGTH_LONG).show();
		}
		
		//l.setOnItemClickListener(clickListener);
		l.setOnItemClickListener(new OnItemClickListener(){
	         public void onItemClick(AdapterView<?> a, View v, int position, long id) { 
	          ListView l = (ListView) findViewById(R.id.albumBrowse);
	          Object o = l.getItemAtPosition(position);
	          AlbumListItem album = (AlbumListItem)o;
	          Intent i = new Intent(AlbumBrowse.this,SongBrowse.class);
	      		i.putExtra("Album",album.getAlbumName());
	      		i.putExtra("AlbumID",album.getAlbumId());
	      		startActivityForResult(i,2);
	          }  
		});
		
		
    }
	/*public void onListItemClick(ListView l, View v, int position, long id){
		super.onListItemClick(l, v, position, id);
		Object o = l.getItemAtPosition(position);
        AlbumListItem album = (AlbumListItem)o;
        Intent i = new Intent(AlbumBrowse.this,SongBrowse.class);
    	i.putExtra("Album",album.getAlbumName());
    	i.putExtra("AlbumID",album.getAlbumId());
    	startActivityForResult(i,2);
	}*/
	
	/*public void onListItemClick(ListView l, View v, int position, long id) {
    	super.onListItemClick(l, v, position, id);
    	String selected = this.albumListAdapter.getItem(position);
    	String query=null;
    	String[] params=null;
    	if(!selected.equals("unknown album")){
		    query = "SELECT AlbumID FROM CoreAlbums WHERE Title=?";
		    params = new String[]{selected};
		    if(!titleText.equals("Albums")){
				query+=" AND ArtistName=?";
				params = new String[]{selected,titleText};
			}
    	}
    	else{
    		query = "SELECT AlbumID FROM CoreAlbums WHERE TitleLowered=?";
		    params = new String[]{selected};
		    if(!titleText.equals("Albums")){
				query+=" AND ArtistName=?";
				params = new String[]{selected,titleText};
			}
    	}
		Cursor c = bansheeDB.rawQuery(query,params);
		//int columnCount = c.getColumnCount();
		int albumIDColumn = c.getColumnIndex("AlbumID");
		//int count = c.getCount();
		c.moveToFirst();
		int albumID=0;
		try{
		albumID = c.getInt(albumIDColumn);
		}
		catch(Exception e){
			
		}
		
    	//Toast.makeText(this,selected,Toast.LENGTH_SHORT).show();
    	Intent i = new Intent(this,SongBrowse.class);
    	i.putExtra("Album",selected);
    	i.putExtra("AlbumID",albumID);
    	startActivityForResult(i,2);
	}*/

}
