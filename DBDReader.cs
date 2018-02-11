﻿using System;
using System.Collections.Generic;
using System.IO;
using static DBDefsLib.Structs;

namespace DBDefsLib
{
    public class DBDReader
    {
        public DBDefinition Read(string file)
        {
            if (!File.Exists(file))
            {
                throw new FileNotFoundException("Unable to find definitions file: " + file);
            }

            var builds = new List<int>();
            var layoutHashes = new List<string>();
            var definitions = new List<Definition>();
            var columnDefinitionDictionary = new Dictionary<string, ColumnDefinition>();
            var versionDefinitions = new List<VersionDefinitions>();

            var lines = File.ReadAllLines(file);
            var lineNumber = 0;

            if (lines[0].StartsWith("COLUMNS"))
            {
                lineNumber++;
                while (true)
                {
                    var line = lines[lineNumber++];

                    // Column definitions are done after encountering a newline
                    if (string.IsNullOrWhiteSpace(line)) break;

                    // Create a new column definition to store information in
                    var columnDefinition = new ColumnDefinition();

                    /* TYPE READING */
                    // List of valid types
                    var validTypes = new List<string> { "uint", "int", "float", "string", "locstring" };

                    // Read line up to space (end of type) or < (foreign key)
                    var type = line.Substring(0, line.IndexOfAny(new char[] { ' ', '<' }));

                    // Check if type is valid, throw exception if not!
                    if (!validTypes.Contains(type))
                    {
                        throw new Exception("Invalid type: " + type + " on line " + lineNumber);
                    }
                    else
                    {
                        columnDefinition.type = type;
                    }

                    /* FOREIGN KEY READING */
                    // Only read foreign key if foreign key identifier is found right after type (it could also be in comments)
                    if (line.StartsWith(type + "<"))
                    {
                        // Read foreign key info between < and > without < and > in result, then split on :: to separate table and field
                        var foreignKey = line.Substring(line.IndexOf('<') + 1, line.IndexOf('>') - line.IndexOf('<') - 1).Split(new string[] { "::" }, StringSplitOptions.None);

                        // There should only be 2 values in foreignKey (table and col)
                        if(foreignKey.Length != 2)
                        {
                            throw new Exception("Invalid foreign key length: " + foreignKey.Length);
                        }
                        else
                        {
                            columnDefinition.foreignTable = foreignKey[0];
                            columnDefinition.foreignColumn = foreignKey[1];
                        }
                    }

                    /* NAME READING */
                    var name = "";
                    // If there's only one space on the line at the same locaiton as the first one, assume a simple line like "uint ID", this can be better
                    if(line.LastIndexOf(' ') == line.IndexOf(' '))
                    {
                        name = line.Substring(line.IndexOf(' ') + 1);
                    }
                    else
                    {
                        // Location of first space (after type)
                        var start = line.IndexOf(' ');

                        // Second space (after name)
                        var end = line.IndexOf(' ', start + 1) - start - 1;

                        name = line.Substring(start + 1, end);
                    }

                    // If name ends in ? it's unverified
                    if (name.EndsWith("?"))
                    {
                        columnDefinition.verified = false;
                        name = name.Remove(name.Length - 1);
                    }
                    else
                    {
                        columnDefinition.verified = true;
                    }

                    /* COMMENT READING */
                    if (line.Contains("//"))
                    {
                        columnDefinition.comment = line.Substring(line.IndexOf("//") + 2).Trim();
                    }

                    // Add to dictionary
                    columnDefinitionDictionary.Add(name, columnDefinition);
                }
            }
            else
            {
                throw new Exception("File does not start with column definitions!");
            }

            return new DBDefinition
            {
                columnDefinitions = columnDefinitionDictionary,
                versionDefinitions = versionDefinitions.ToArray()
            };
        }
    }
}
