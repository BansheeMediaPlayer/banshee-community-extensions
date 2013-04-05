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

import java.io.FileInputStream;
import java.io.InputStreamReader;
import java.io.OutputStream;
import java.net.Socket;
import java.util.ArrayList;
import android.app.ListActivity;
import android.content.Context;
import android.content.Intent;
import android.os.Bundle;
import android.view.View;
import android.widget.ArrayAdapter;
import android.widget.ListView;
import android.widget.Toast;


public class BansheeRemote extends ListActivity {
    String serverData = null; 
    String savedServers[];
    boolean serversExist=false;
    ArrayList<String> savedServersListArray;
    ArrayAdapter<String> savedServersListAdapter; 
    final String filename = "bansheeServers.dat";
    
    
    public String readSettings(Context context, String filename){
        FileInputStream fIn = null;
        InputStreamReader isr = null;
       
        char[] inputBuffer = new char[255];
        String data = null;
       
        try{
         fIn = openFileInput(filename);      
            isr = new InputStreamReader(fIn);
            isr.read(inputBuffer);
            data = new String(inputBuffer);
            }
            catch (Exception e) {      
            e.printStackTrace();
            }
            finally {
               try {
                      isr.close();
                      fIn.close();
                      } catch (Exception e) {
                      e.printStackTrace();
                      }
            }
            return data;
       } 
	@Override
    protected void onActivityResult(int requestCode, int resultCode, Intent data){
		super.onActivityResult(requestCode, resultCode, data);
		finish();
	}
	
	
    @Override
    public void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        setup();
    }
    public void setup(){
    	this.savedServersListArray = new ArrayList<String>();
        this.savedServersListAdapter = new ArrayAdapter<String>(this,android.R.layout.simple_list_item_1,savedServersListArray);
        setListAdapter(savedServersListAdapter);
        serverData = readSettings(BansheeRemote.this,filename);
    	if(serverData!=null){
    		serversExist=true;
    		savedServers = serverData.split(";");
    		for(int i=savedServers.length-2;i>=0;i--){
    			savedServersListArray.add(savedServers[i]);
    		}
    	}
    	savedServersListArray.add("Add new Server");
    	//File root = Environment.getDataDirectory();
    	//Toast.makeText(BansheeRemote.this,root.getAbsolutePath(),Toast.LENGTH_LONG).show();
    }
    
    public void onListItemClick(ListView l, View v, int position, long id) {
    	super.onListItemClick(l, v, position, id);
    	String selected = this.savedServersListAdapter.getItem(position);
    	if(selected.equals("Add new Server")){
    		Intent response = new Intent(BansheeRemote.this,NewServer.class);
    		startActivityForResult(response,1);
    	}
    	else{
    		boolean canConnect = false;
    		String server[] = selected.split(":");
    		String ip = server[0];
    		int port = Integer.parseInt(server[1]);
    		String command = "test/";
    		Socket s;
			OutputStream os;
    		try {
    			s = new Socket(ip,port); 
    			os = s.getOutputStream();
    			os.write(command.getBytes(), 0, command.length());
    			s.close();
    			os.close();
    			canConnect = true;
    		} catch(Exception e) {
    			canConnect = false;
    			Toast.makeText(BansheeRemote.this,"Can't connect to Server. Check your settings.",Toast.LENGTH_SHORT).show();
    		}
			
			if(canConnect){
				Intent response = new Intent(BansheeRemote.this,main.class);
				response.putExtra("ip",ip);
				response.putExtra("port", port);		
				startActivityForResult(response,1);
			}
    	}
    	//Toast.makeText(BansheeRemote.this, selected,Toast.LENGTH_SHORT).show();
    }

}
