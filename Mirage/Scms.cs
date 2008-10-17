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
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;


namespace Mirage
{
	public class ScmsImpossibleException : Exception
	{
	}
    
	[Serializable]
	public class Scms
	{
	    Vector mean = null;
	    CovarianceMatrix cov = null;
	    CovarianceMatrix icov = null;

	    public static Scms GetScms(Matrix mfcc)
	    {
	        DbgTimer t = new DbgTimer();
	        t.Start();
	        
	        Scms s = new Scms();
	        
	        // Mean
	        s.mean = mfcc.Mean();

			// Covariance
	        Matrix fullCov = mfcc.Covariance(s.mean);
        	s.cov = new CovarianceMatrix(fullCov);
	        for (int i = 0; i < s.cov.dim; i++) {
	            for (int j = i+1; j < s.cov.dim; j++) {
	                s.cov.d[i*s.cov.dim+j-(i*i+i)/2] *= 2;
	            }
	        }
	        
	        // Inverse Covariance
	        try {
        		Matrix fullIcov = fullCov.Inverse();
        		s.icov = new CovarianceMatrix(fullIcov);
	        } catch (MatrixSingularException) {
	        	throw new ScmsImpossibleException();
	        }
	        
	        long stop = 0;
	        t.Stop(ref stop);
	        Dbg.WriteLine("Mirage - scms created in: {0}ms", stop);
	        
	        return s;
	    }
	    
	    public float Distance(Scms scms2)
	    {
	        float val = 0;

	        unsafe {
	            int i;
	            int k;
	            int idx = 0;
	            int dim = cov.dim;
	            int covlen = (dim*dim+dim)/2;
	            float tmp1;

	            fixed (float* s1cov = cov.d, s2icov = scms2.icov.d,
	                    s1icov = icov.d, s2cov = scms2.cov.d,
	                    s1mean = mean.d, s2mean = scms2.mean.d)
	            {
	                float* mdiff = stackalloc float[dim];
	                float* aicov = stackalloc float[covlen];


	                for (i = 0; i < covlen; i++) {
	                    val += s1cov[i] * s2icov[i] + s2cov[i] * s1icov[i];
	                    aicov[i] = s1icov[i] + s2icov[i];
	                }

	                for (i = 0; i < dim; i++) {
	                    mdiff[i] = s1mean[i] - s2mean[i];
	                }

	                for (i = 0; i < dim; i++) {
	                    idx = i - dim;
	                    tmp1 = 0;

	                    for (k = 0; k <= i; k++) {
	                        idx += dim - k;
	                        tmp1 += aicov[idx] * mdiff[k];
	                    }
	                    for (k = i + 1; k < dim; k++) {
	                        idx++;
	                        tmp1 += aicov[idx] * mdiff[k];
	                    }
	                    val += tmp1 * mdiff[i];
	                }
	            }
	        }

	        return val;
	    }

	    public byte[] ToBytes()
	    {
	        MemoryStream stream = new MemoryStream();
	        BinaryFormatter bformatter = new BinaryFormatter();
	        bformatter.Serialize(stream, this);
	        stream.Close();
	        
	        return stream.ToArray();
	    }
	    
	    public static Scms FromBytes(byte[] buf)
	    {
	        MemoryStream stream = new MemoryStream(buf);
	        BinaryFormatter bformatter = new BinaryFormatter();
	        
	        Scms scms = (Scms)bformatter.UnsafeDeserialize(stream, null);
	        stream.Close();
	        
	        return scms;

	    }
	}

}
