/*
 * Mirage - High Performance Music Similarity and Automatic Playlist Generator
 * http://hop.at/mirage
 * 
 * Copyright (C) 2007-2008 Dominik Schnitzer <dominik@schnitzer.at>
 * 
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU General Public License
 * as published by the Free Software Foundation; either version 2
 * of the License, or (at your option) any later version.
 * 
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 51 Franklin Street, Fifth Floor,
 * Boston, MA  02110-1301, USA.
 */


using System;
using System.Resources;
using System.IO;
using System.Reflection;

namespace Mirage
{

	public class MfccFailedException : Exception
	{
	}

	public class Mfcc
	{

	    Matrix filterWeights;
	    Matrix dct;
	    
	    public Mfcc(int winsize, int srate, int filters, int cc)
	    {
	        Assembly assem = this.GetType().Assembly;
	        
	        // Load the DCT
	        dct = Matrix.Load(assem.GetManifestResourceStream("dct.filter"));
	            
	        // Load the MFCC filters from the filter File.
	        filterWeights = Matrix.Load(assem.GetManifestResourceStream("filterweights.filter"));
	    }
	    
	    public Matrix Apply(Matrix m)
	    {

	        DbgTimer t = new DbgTimer();
	        t.Start();

	        Matrix mel = new Matrix(filterWeights.rows, m.columns);
	        
	        /* Performance optimization of ...

	        mel = filterWeights.Multiply(m);
	        for (int i = 0; i < mel.rows; i++) {
	            for (int j = 0; j < mel.columns; j++) {
	                mel.d[i, j] = (mel.d[i, j] < 1.0f ?
	                                0 : (float)(10.0 * Math.Log10(mel.d[i, j])));
	            }
	        }*/
	        
	        unsafe {

	            int mc = m.columns;
	            int mr = m.rows;
	            int melcolumns = mel.columns;
	            int fwc = filterWeights.columns;
	            int fwr = filterWeights.rows;
	            int idx;
	            int i;
	            int k;
	            int j;
	            int kfwc;
	            
	            fixed (float* md = m.d, fwd = filterWeights.d, meld = mel.d) {
	                for (i = 0; i < mc; i++) {
	                    for (k = 0; k < fwr; k++) {
	                        idx = k*melcolumns + i;
	                        kfwc = k*fwc;

	                        for (j = 0; j < mr; j++) {
	                            meld[idx] += fwd[kfwc + j] * md[j*mc + i];
	                        }

	                        meld[idx] = (meld[idx] < 1.0f ?
	                                0 : (float)(10.0 * Math.Log10(meld[idx])));
	                    }
	                    
	                }
	            }
	        }
	        
	        try {
		        Matrix mfcc = dct.Multiply(mel);
		        
		        long stop = 0;
		        t.Stop(ref stop);
		        Dbg.WriteLine("Mirage - mfcc Execution Time: {0}ms", stop);
		        
		        return mfcc;
		        
		    } catch (MatrixDimensionMismatchException) {
		    	throw new MfccFailedException();
		    }
	    }
	    
	}

}
