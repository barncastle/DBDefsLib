﻿using System.Collections.Generic;
using System.IO;
using static DBDefsLib.Structs;

namespace DBDefsLib
{
    public class DBDWriter
    {
        public void Save(DBDefinition definition, string target)
        {
            if (!Directory.Exists(Path.GetDirectoryName(target)))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(target));
            }

            using (StreamWriter writer = new StreamWriter(target))
            {
                writer.NewLine = "\n";

                writer.WriteLine("COLUMNS");
                foreach(var columnDefinition in definition.columnDefinitions)
                {
                    if(columnDefinition.Value.type == "uint")
                    {
                        writer.Write("int");
                    }
                    else
                    {
                        writer.Write(columnDefinition.Value.type);
                    }

                    if (!string.IsNullOrEmpty(columnDefinition.Value.foreignTable) && !string.IsNullOrEmpty(columnDefinition.Value.foreignColumn))
                    {
                        writer.Write("<" + columnDefinition.Value.foreignTable + "::" + Utils.NormalizeColumn(columnDefinition.Value.foreignColumn) + ">");
                    }

                    var normalizedColumnName = Utils.NormalizeColumn(columnDefinition.Key);
                    writer.Write(" " + normalizedColumnName);
                    if (definition.columnDefinitions[columnDefinition.Key].type == "locstring" && !columnDefinition.Key.EndsWith("_lang"))
                    {
                        writer.Write("_lang");
                    }

                    if (columnDefinition.Value.verified == false)
                    {
                        writer.Write("?");
                    }

                    if (!string.IsNullOrWhiteSpace(columnDefinition.Value.comment))
                    {
                        writer.Write(" // " + columnDefinition.Value.comment);
                    }

                    writer.Write(writer.NewLine);
                }

                // New line between COLUMNS and BUILD definitions
                writer.Write(writer.NewLine);

                for (var i = 0; i < definition.versionDefinitions.Length; i++)
                {
                    var versionDefinition = definition.versionDefinitions[i];
                    if(versionDefinition.layoutHashes.Length > 0)
                    {
                        writer.WriteLine("LAYOUT " + string.Join(", ", versionDefinition.layoutHashes));
                    }

                    if(versionDefinition.builds.Length > 0)
                    {
                        writer.Write("BUILD ");
                        var sortedVersionlist = new List<Build>();
                        sortedVersionlist.AddRange(versionDefinition.builds);
                        sortedVersionlist.Sort();
                        versionDefinition.builds = sortedVersionlist.ToArray();
                        for(var b =0; b < versionDefinition.builds.Length; b++)
                        {
                            writer.Write(versionDefinition.builds[b].ToString());
                            if(b + 1 < versionDefinition.builds.Length)
                            {
                                writer.Write(", ");
                            }
                        }
                        writer.Write(writer.NewLine);
                    }

                    if (!string.IsNullOrWhiteSpace(versionDefinition.comment))
                    {
                        writer.WriteLine("COMMENT " + versionDefinition.comment);
                    }

                    if (versionDefinition.buildRanges.Length > 0)
                    {
                        foreach(var buildRange in versionDefinition.buildRanges)
                        {
                            writer.WriteLine("BUILD " + buildRange.ToString());
                        }
                    }

                    foreach(var column in versionDefinition.definitions)
                    {
                        if (column.isID || column.isNonInline || column.isRelation)
                        {
                            writer.Write("$");

                            if (column.isNonInline)
                            {
                                if (column.isID)
                                {
                                    writer.Write("noninline,id");
                                }
                                else if (column.isRelation)
                                {
                                    writer.Write("noninline,relation");
                                }
                            }
                            else
                            {
                                if (column.isID)
                                {
                                    writer.Write("id");
                                }
                                else if (column.isRelation)
                                {
                                    writer.Write("relation");
                                }
                            }

                            writer.Write("$");
                        }

                        var normalizedColumnName = Utils.NormalizeColumn(column.name);

                        writer.Write(normalizedColumnName);

                        // locstrings should always have _lang
                        if(definition.columnDefinitions[column.name].type == "locstring" && !column.name.EndsWith("_lang"))
                        {
                            writer.Write("_lang");
                        }

                        if(column.size > 0)
                        {
                            if (column.isSigned)
                            {
                                writer.Write("<" + column.size + ">");
                            }
                            else
                            {
                                writer.Write("<u" + column.size + ">");
                            }
                        }

                        if(column.arrLength > 0)
                        {
                            writer.Write("[" + column.arrLength + "]");
                        }

                        if (!string.IsNullOrWhiteSpace(column.comment))
                        {
                            writer.Write(" // " + column.comment);
                        }

                        writer.Write(writer.NewLine);
                    }

                    if (i + 1 < definition.versionDefinitions.Length)
                    {
                        writer.Write(writer.NewLine);
                    }
                }

                writer.Flush();
                writer.Close();
            }
        }
    }
}
