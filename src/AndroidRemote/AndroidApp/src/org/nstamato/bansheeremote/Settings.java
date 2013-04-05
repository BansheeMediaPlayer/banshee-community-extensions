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

import java.io.OutputStream;
import java.net.Socket;

import android.app.Activity;
import android.content.Intent;
import android.os.Bundle;
import android.view.View;
import android.view.View.OnClickListener;
import android.widget.Button;
import android.widget.EditText;
import android.widget.Toast;


public class Settings extends Activity {
	
	//public void onConfigurationChanged(Configuration newConfig){
		//setup();
	//}
	
    @Override
    public void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        setup();
    }
    public void setup(){
    	setContentView(R.layout.init);
        Button data = (Button) findViewById(R.id.submit);

        data.setOnClickListener(new OnClickListener(){

			public void onClick(View v) {
				EditText iptext = (EditText)findViewById(R.id.ip);
		        EditText porttext = (EditText)findViewById(R.id.port);
				String ip = iptext.getText().toString();
				Socket s;
				OutputStream os;
				String command = "test/";
				boolean canConnect = false;
				int port=Integer.parseInt(porttext.getText().toString());
	    		try 
	    		{
	    			s = new Socket(ip,port); 
	    			os = s.getOutputStream();
	    			os.write(command.getBytes(), 0, command.length());
	    			s.close();
	   				os.close();
	 				canConnect = true;
	 			} 
	    		catch(Exception e) 
	    		{
	  				canConnect = false;
	   				Toast.makeText(Settings.this,"Can't connect to Server. Check your settings.",Toast.LENGTH_SHORT).show();
	   			}
				
				if(canConnect){
					Intent response = new Intent();
					response.putExtra("ip",ip);
					response.putExtra("port", port);
					setResult(RESULT_OK,response);
					finish();
				}
			}
        	
        });
    }

}
