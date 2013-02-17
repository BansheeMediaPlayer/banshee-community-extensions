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

import java.io.ByteArrayOutputStream;
import java.io.File;
import java.io.FileOutputStream;
import java.io.InputStream;
import java.io.OutputStream;
import java.net.Socket;

import android.app.Activity;
import android.app.ProgressDialog;
import android.content.Intent;
import android.content.res.Configuration;
import android.database.sqlite.SQLiteException;
import android.os.Bundle;
import android.os.Environment;
import android.os.Handler;
import android.os.Message;
import android.util.Log;

public class Sync extends Activity{
public ProgressDialog pd;
String server;
int port;
public final String filenameDB = "banshee.db";
public static ByteArrayOutputStream byteArrayOutputStream = new ByteArrayOutputStream();
public static byte[] byteBuff = new byte[1024];

private Handler dbHandler = new Handler() {
    @Override
    public void handleMessage(Message msg) {
    	//int progress = msg.arg1;
    	//pd.setProgress(progress);
    	//if(progress>=100)
            pd.dismiss();
            //tv.setText(pi_string);

    }
};

	public void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        Bundle extras = getIntent().getExtras();
        server = extras.getString("ip");
    	port = extras.getInt("port");
        sync();
    }
	
	public void onConfigurationChanged(Configuration newConfig) {
	  super.onConfigurationChanged(newConfig);
	  sync();
	}
	
	
	public void sync(){
		pd = new ProgressDialog(this);
		pd.setCancelable(true);
		//pd.setProgressStyle(ProgressDialog.STYLE_HORIZONTAL);
		pd.setTitle("Syncing with Banshee...");
		pd.setMessage("This might take some time, depending on your internet connection and your phone.");
		pd.show();
		
		Thread getDB  = new Thread(new Runnable (){
    		//String command="sync/";
    		public void run(){
		    	try {
		    		//String dbCount = getInfo("syncCount",null);
		    		//int totalReceived = 0;
		    		//int remainingBytes = getInfoInt("syncCount",null);
		    		int chunksize=8000;//remainingBytes;
		    		//int offset = 0;
		    		//Log.i("banshee","bytes "+remainingBytes);
		    		//Toast.makeText(main.this,"Bytes "+dbCount,Toast.LENGTH_SHORT).show();
		    		byte[] dbbytes = null;
		    		File root = Environment.getExternalStorageDirectory();
		    		//Log.i("banshee","external directory "+root.getAbsolutePath());
		    		FileOutputStream fstream = new FileOutputStream(new File(root,filenameDB));
		    	    //FileOutputStream fstream = openFileOutput(filenameDB,Context.MODE_PRIVATE);
		    	    dbbytes = new byte[chunksize];
		    	    Socket s = new Socket(server,port);
	    	    	OutputStream os = s.getOutputStream();
	    	    	InputStream is = s.getInputStream();
	    	    	String command = "sync/";
	    	    	os.write(command.getBytes(), 0, command.length());
	    	    	//Log.i("banshee","sent command");
	    	    	int len=0;
	    	    	while ( (len = is.read(dbbytes)) > 0 ) {
	    	    		//Log.i("banshee","inside loop");
	    	    		//is.read(dbbytes,0,chunksize);
	    	            fstream.write(dbbytes,0,len);
	    	            //totalReceived+=len;
	    	            //int progress=totalReceived/remainingBytes*100;
	    	            //Message progressMessage = new Message();
	    	            //progressMessage.arg1=progress;
	    	            //dbHandler.sendMessage(progressMessage);
	    	       }
	    	    	
		    	    /*while(remainingBytes>0){
		    	    	Socket s = new Socket(server,port);
		    	    	OutputStream os = s.getOutputStream();
		    	    	InputStream is = s.getInputStream();
		    	    	String offsetT = Integer.toString(offset);
		    	    	String command = "sync/"+offsetT;
		    	    	//String command = "sync/";
		    	    	os.write(command.getBytes(), 0, command.length());
		    	    	//if(remainingBytes>=chunksize){
		    	    		//dbbytes = new byte[chunksize];
		    	    		is.read(dbbytes,0,chunksize);
		    	    		//if(rep==0){
		    	    			//hash = Base64.encodeToString(dbbytes, 0, chunksize, Base64.DEFAULT);
		    	    			//os.write(hash.getBytes(),0,chunksize);
		    	    		//}
		    	    		remainingBytes-=chunksize;
		    	    		offset+=chunksize;
		    	    	//}
		    	    	//else{
		    	    		//dbbytes = new byte[remainingBytes];
		    	    		//is.read(dbbytes,0,remainingBytes);
		    	    		//remainingBytes=0;
		    	    	//}
					//Toast.makeText(main.this,"hello "+dbCount,Toast.LENGTH_SHORT).show();
		    	    	fstream.write(dbbytes);
		    	    	s.close();
		    	    	//rep++;
		    	    }*/
		    	    fstream.close();
		    	    dbHandler.sendEmptyMessage(0);
		    	    Intent response = new Intent();
					//response.putExtra("ip",ip);
					//response.putExtra("port", port);
					setResult(RESULT_OK,response);
					finish();
		    	    //continueserver=true;
		    	    //bansheeDB = SQLiteDatabase.openDatabase(filenameDB, null, SQLiteDatabase.OPEN_READONLY);
		    	   	} 
		    	catch (java.lang.OutOfMemoryError e) {
		    		Log.i("banshee","ran out of memory");
		    		dbHandler.sendEmptyMessage(0);
		    		Intent response = new Intent();
		    		setResult(RESULT_OK+2,response);
		    		finish();
		    		//Toast.makeText(Sync.this,"Memory Error.",Toast.LENGTH_SHORT).show();
		    		
		    	}
		    	catch(SQLiteException e){
		    		dbHandler.sendEmptyMessage(0);
		    		Intent response = new Intent();
		    		setResult(RESULT_OK+1,response);
		    		finish();
		    		//Toast.makeText(main.this,"Can't create database",Toast.LENGTH_LONG).show();
		    	}
		    	
		    	catch (Exception e) {
		    		dbHandler.sendEmptyMessage(0);
		    		Intent response = new Intent();
		    		setResult(RESULT_OK+1,response);
		    		finish();
		    		//Toast.makeText(Sync.this,"Can't write to database file.",Toast.LENGTH_SHORT).show();
		    	}
    		}
    	});
		getDB.start();
		
	}
	public String getInfo(String action, String params) throws Exception{
    	String data=null;
    	String formattedAction = action+'/'+params;
    		Socket s = new Socket(server,port);
			OutputStream os = s.getOutputStream();
			InputStream is =  s.getInputStream();
			os.write(formattedAction.getBytes(), 0, formattedAction.length());
			int byteCount = -1;
			byteArrayOutputStream.reset();
			while ((byteCount = is.read(byteBuff, 0, byteBuff.length)) != -1) {
				byteArrayOutputStream.write(byteBuff, 0, byteCount);
			}
			data = byteArrayOutputStream.toString();
		return data;
    }
	public int getInfoInt(String action,String params){
    	int n=-1;
    	boolean done=false;
    	while(!done){
	    	try{
	    		n = Integer.parseInt(getInfo(action,params));
	    		done=true;
	    	}
	    	catch(NumberFormatException e){
	    		done=false;
	    	}
	    	catch(Exception e){
	    		
	    	}
    	}
		return n;
    }
}
