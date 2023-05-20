using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualBasic.FileIO;

namespace flarmcfg
{
    class CSV_helper
    {
        private Dictionary<string, int> ?_Dict = new Dictionary<string, int>();
        private List<string[]> ?_Data = new List<string[]>();

        public Dictionary<string, int> Dict {get { return _Dict;} }
        public List<string[]> Data { get { return _Data; } }

        public CSV_helper(string fileName)
        {
            try
            {
                using (TextFieldParser csvParser = new TextFieldParser(fileName))
                {
                    string[] ?fields;
                    csvParser.CommentTokens = new string[] { "#" };
                    csvParser.SetDelimiters(new string[] { "," });
                    csvParser.HasFieldsEnclosedInQuotes = false;

                    //creating a dictionary
                    if (!csvParser.EndOfData)
                    {
                        fields = csvParser.ReadFields();
                        if (fields != null)
                        {
                            for (int index = 0; index < fields.Length; index++)
                            {
                                _Dict[fields[index]] = index;
                            }
                        }
                    }


                    while (!csvParser.EndOfData)
                    {
                        // Read current line fields, pointer moves to the next line.
                        fields = csvParser.ReadFields();
                        if(fields != null)
                        {
                            _Data.Add(fields);
                        }
                    }
                }
            }catch { _Data = null; _Dict = null; }
        }
    }
}
