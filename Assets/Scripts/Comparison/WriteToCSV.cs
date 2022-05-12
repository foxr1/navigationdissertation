using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Used for writing comparison results to CSV file
namespace WriteToCSVFile
{
    class WriteToCSV
    {
        public static void addRecord(int ID, float timeTaken, float totalTime, float meanTimeTaken, string filepath)
        {
            try
            {
                using (System.IO.StreamWriter file = new System.IO.StreamWriter(@filepath, true))
                {
                    file.WriteLine(ID + ", " + timeTaken + ", " + totalTime + ", " + meanTimeTaken);
                }
            }
            catch (Exception ex)
            {
                throw new ApplicationException("Error: ", ex);
            }
        }
    }
}