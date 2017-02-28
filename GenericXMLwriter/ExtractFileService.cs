using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using System.Threading;
using GenericXmlCreator.Interfaces;

namespace GenericXmlCreator
{
    public class ExtractFileService : IDisposable, IExtractFileService
    {
        #region Class variables

        /// <summary>
        /// temp folder for output extract file to be built into
        /// </summary>
        //public string workingFolder = string.Empty;

        /// <summary>
        /// Mapping Configuration File info
        /// </summary>
        public FileInfo mappingConfigurationInfoFile { get; set; }

        /// <summary>
        /// OutputFormat Sample File info
        /// </summary>
        public FileInfo outputFormatInfoFile { get; set; }


        private DataSet finalOutputDataSet = new DataSet();

        private DataSet InputDataSet = new DataSet();

        private bool nodeFound = false;
        private XElement curNode = null;
        private string mappingOutputFormatFilePath = "";
        private Dictionary<string, int> curValues = null;
        private List<string> colsToBeUnique = null;
        private Dictionary<string, bool> colsToDiffer = null;
        private Dictionary<string, string> colsToDifferVals = null;
        private bool found = false;
        string strColsToBeUnique = "";


        #endregion

        /// <summary>
        /// Initializes a new instance of the <see cref="ExtractFileService" /> class.
        /// </summary>        
        /// <param name="parameters">Parameter Collection</param>
        public ExtractFileService()
        {

        }

        private void SetConfigurationValues(string Type)
        {
            mappingConfigurationInfoFile = new FileInfo(ConfigurationManager.AppSettings["FileMappingConfigs"] + Type + ".xml");
            outputFormatInfoFile = new FileInfo(ConfigurationManager.AppSettings["FileMappingConfigs"] + Type + "_OutputFormat" + ".xml");
            //this.workingFolder = Path.Combine(ConfigurationManager.AppSettings["WorkFolder"], ConfigurationManager.AppSettings[Type]);            

        }

        /// <summary>
        /// Loop over data table rows and create new XML strings based on configuration.
        /// Config file should have AppSetting key "FileMappingConfigs" set to the folder where configuration file <processName>.xml and <processName>_OutputFormat.xml exist
        /// </summary>
        /// <param name="MainDataDt"></param>
        /// <param name="processName"></param>
        /// <returns></returns>
        public List<string> WriteRecord(DataTable MainDataDt, string processName)
        {
            List<string> xmls = new List<string>();

            try
            {
                SetConfigurationValues(processName);

                if (MainDataDt.Rows.Count == 0)
                    return xmls;

                finalOutputDataSet.ReadXml(outputFormatInfoFile.FullName);

                mappingOutputFormatFilePath = outputFormatInfoFile.FullName;

                XDocument configDoc = XDocument.Load(mappingConfigurationInfoFile.FullName);

                strColsToBeUnique = configDoc.Root.Element("Layout").Element("DbColumnsThatShouldCombineUnique").Value;
                //finalXmlFolderLocation = configDoc.Root.Element("Layout").Element("FinalXmlFolderLocation").Value;
                //if (!Directory.Exists(finalXmlFolderLocation))
                //{
                //    Directory.CreateDirectory(finalXmlFolderLocation);
                //}
                colsToBeUnique = strColsToBeUnique.Split(',').ToList<string>();
                colsToDiffer = new Dictionary<string, bool>();
                colsToDifferVals = new Dictionary<string, string>();
                foreach (string str in strColsToBeUnique.Split(','))
                {
                    colsToDiffer.Add(str, false);
                    colsToDifferVals.Add(str, "");
                }

                string xmlFileBasedOn = configDoc.Root.Element("Layout").Element("NewXMLForEachNewValue").Value;
                string[] param = xmlFileBasedOn.Split(',');
                DataView view = new DataView(MainDataDt);
                DataTable distinctValues = null;

                mapColsToXmlNodes();
                if (!string.IsNullOrEmpty(xmlFileBasedOn))
                {
                    distinctValues = view.ToTable(true, param);

                    foreach (DataRow row in distinctValues.Rows)
                    {
                        string rowFilter = "";
                        for (int i = 0; i < param.Length; i++)
                        {
                            if (i == param.Length - 1)
                            {
                                rowFilter += param[i] + "= '" + row[param[i]].ToString() + "'";
                            }
                            else
                            {
                                rowFilter += param[i] + "= '" + row[param[i]].ToString() + "' AND ";
                            }
                        }
                        DataView dv = new DataView(MainDataDt);
                        dv.RowFilter = rowFilter;
                        finalOutputDataSet.Clear();
                        finalOutputDataSet.ReadXml(outputFormatInfoFile.FullName);
                        string res = ProcessRecord(dv);
                        if (!string.IsNullOrEmpty(res))
                        {
                            xmls.Add(res);
                        }
                    }
                }
                else
                {
                    finalOutputDataSet.Clear();
                    finalOutputDataSet.ReadXml(outputFormatInfoFile.FullName);
                    string res = ProcessRecord(view);
                    if (!string.IsNullOrEmpty(res))
                    {
                        xmls.Add(res);
                    }
                }

            }
            catch (Exception ex)
            {
                throw ex;
            }
            return xmls;
        }

        /// <summary>
        /// Sort the Data table and Create XML
        /// </summary>
        /// <param name="dt">Data table with rows to be written</param>
        /// <param name="docSelection">XML's Document Selection node object</param>
        /// <param name="printData">XML's Print Data node object</param>
        /// <returns>True if success</returns>
        private string ProcessRecord(DataView dv)
        {
            string returnXml = null;

            try
            {
                if (dv == null)
                {
                    return returnXml;
                }

                dv.Sort = strColsToBeUnique;
                DataTable inputData = dv.ToTable();
                curValues = new Dictionary<string, int>();

                FillCurValues();
                foreach (DataRow row in inputData.Rows)
                {
                    foreach (string str in strColsToBeUnique.Split(','))
                    {
                        colsToDiffer[str] = false;
                    }
                    List<DataColumn> dbColumns = new List<DataColumn>();
                    foreach (var _o in colsToBeUnique)
                    {
                        dbColumns.Add(inputData.Columns[_o]);
                    }
                    foreach (DataColumn dbcol in inputData.Columns)
                    {
                        if (!dbColumns.Any(t => t.ColumnName == dbcol.ColumnName))
                        {
                            dbColumns.Add(dbcol);
                        }
                    }
                    foreach (DataColumn dbcol in dbColumns)
                    {
                        MarkColumnThatRequireNewLine(dbcol.ColumnName, row);

                        foreach (DataTable dt in finalOutputDataSet.Tables)
                        {
                            found = false;
                            UpdateRowInFinalDataTable(dt, row, dbcol.ColumnName);
                            if (found)
                            {
                                FillIds();
                                break;
                            }
                        }
                    }
                }



                // if (_FileGenerationReqd)
                //finalOutputDataSet.WriteXml(this.workingFolder + "\\" + cnt + ".xml");
                //else
                //{
                StringWriter sw = new StringWriter();
                finalOutputDataSet.WriteXml(sw);
                returnXml = sw.ToString();
                //}

            }
            catch (Exception ex)
            {
                throw ex;
            }

            return returnXml;
        }

        private void FillCurValues()
        {
            foreach (DataTable dt in finalOutputDataSet.Tables)
            {
                if (!curValues.ContainsKey(dt.TableName + "_Id"))
                {
                    curValues.Add(dt.TableName + "_Id", 0);
                }
            }
        }

        /// <summary>
        /// Fill _Id values for all tables of dataset based on other primary -foreign key relation values
        /// </summary>
        private void FillIds()
        {
            foreach (DataTable _dt in finalOutputDataSet.Tables)
            {
                foreach (DataColumn _col in _dt.Columns)
                {
                    if (_col.ColumnName.EndsWith("_Id"))
                    {
                        string _val = _dt.Rows[_dt.Rows.Count - 1][_col.ColumnName].ToString();
                        if (curValues.ContainsKey(_col.ColumnName))
                        {
                            if (_val != curValues[_col.ColumnName].ToString())
                            {
                                if (_val == "")
                                {
                                    _dt.Rows[_dt.Rows.Count - 1][_col.ColumnName] = curValues[_col.ColumnName].ToString();
                                }
                                else
                                {
                                    if (Convert.ToInt32(_val) > Convert.ToInt32(curValues[_col.ColumnName]))
                                    {
                                        if (_dt.TableName != _col.ColumnName.Substring(0, _col.ColumnName.Length - 3))
                                        {
                                            _dt.Rows[_dt.Rows.Count - 1][_col.ColumnName] = curValues[_col.ColumnName].ToString();
                                        }
                                        curValues[_col.ColumnName] = Convert.ToInt32(_val);
                                    }
                                    else
                                    {
                                        DataRow _dr = _dt.NewRow();
                                        _dt.Rows.Add(_dr);

                                        _dt.Rows[_dt.Rows.Count - 1][_col.ColumnName] = curValues[_col.ColumnName].ToString();

                                        if (_dt.Columns.Contains(_dt.TableName + "_Id"))
                                        {
                                            if (!curValues.ContainsKey(_dt.TableName + "_Id"))
                                            {
                                                curValues.Add(_dt.TableName + "_Id", 0);
                                            }
                                            else
                                            {
                                                curValues[_dt.TableName + "_Id"] = curValues[_dt.TableName + "_Id"] + 1;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        private void mapColsToXmlNodes()
        {
            XDocument doc = XDocument.Load(mappingOutputFormatFilePath);

            foreach (DataTable tbl in finalOutputDataSet.Tables)
            {
                nodeFound = false;
                getElementByName(tbl.TableName, doc.Root);
                if (curNode != null)
                {
                    foreach (DataColumn col in tbl.Columns)
                    {
                        if (!col.ColumnName.EndsWith("_Id"))
                        {
                            XElement e = curNode.Element(col.ColumnName);

                            if (e != null)
                            {
                                string dbNameofXmlField = e.Value.ToString();
                                col.Caption = dbNameofXmlField;
                            }
                            else if (col.ColumnName == curNode.Name + "_Text")
                            {
                                string dbNameofXmlField = curNode.Value.ToString();
                                col.Caption = dbNameofXmlField;
                            }
                            else if (curNode.HasAttributes)
                            {
                                XAttribute att = curNode.Attributes().FirstOrDefault(t => t.Name.LocalName == col.ColumnName);
                                if (att != null)
                                {
                                    string dbNameofXmlField = att.Value.ToString();
                                    col.Caption = dbNameofXmlField;
                                }
                            }
                        }
                    }
                }
            }
        }

        private void getElementByName(string name, XElement node)
        {
            if (nodeFound)
                return;
            if (node.HasElements)
            {
                if (node.Name == name)
                {
                    curNode = node;
                    nodeFound = true;
                }
                else
                {
                    foreach (XElement ele in node.Elements())
                    {
                        getElementByName(name, ele);
                    }
                }
            }
            else
            {
                if (node.Name == name)
                {
                    curNode = node;
                    nodeFound = true;
                }
            }
        }

        /// <summary>
        /// sets colsTodiffer[columnName] value to true if there is new value meaning new node to be created in xml for this row.
        /// </summary>
        /// <param name="columnName"></param>
        /// <param name="row"></param>
        private void MarkColumnThatRequireNewLine(string columnName, DataRow row)
        {
            if (colsToDiffer.Keys.Contains(columnName))
            {
                string curVal = "";
                for (int i = 0; i < colsToDiffer.Keys.Count; i++)
                {
                    bool f = false;
                    if (colsToDiffer.Keys.ElementAt(i) == columnName)
                    {
                        f = true;
                        curVal = "";
                        for (int j = 0; j <= i; j++)
                        {
                            string colName = colsToDiffer.Keys.ElementAt(j);
                            curVal = curVal + row[colName].ToString();
                        }
                    }
                    if (f)
                        break;
                }

                if (curVal != colsToDifferVals[columnName])
                {
                    colsToDiffer[columnName] = true;
                    colsToDifferVals[columnName] = curVal;// row[dbcol.ColumnName].ToString();
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="dt"></param>
        /// <param name="row"></param>
        /// <param name="colName">name of column in database</param>
        private void UpdateRowInFinalDataTable(DataTable dt, DataRow row, string colName)
        {
            foreach (DataColumn col in dt.Columns)
            {
                if (col.Caption == colName)
                {
                    found = true;
                    if (dt.Rows.Count == 1 && dt.Rows[dt.Rows.Count - 1][col.ColumnName].ToString() == col.Caption.ToString())
                    {
                        dt.Rows[0][col.ColumnName] = row[col.Caption].ToString();
                    }
                    else if (colsToDiffer.Keys.Contains(colName))
                    {
                        if (colsToDiffer[colName])
                        {
                            if (dt.Rows[dt.Rows.Count - 1][col.ColumnName].ToString() != "")
                            {
                                DataRow dr = dt.NewRow();
                                dt.Rows.Add(dr);
                            }
                            else if (dt.Columns.Contains(dt.TableName + "_Id"))
                            {
                                if (!curValues.ContainsKey(dt.TableName + "_Id"))
                                {
                                    curValues.Add(dt.TableName + "_Id", 0);
                                }
                                else
                                {
                                    int _val = Convert.ToInt32(dt.Rows[dt.Rows.Count - 1][dt.TableName + "_Id"].ToString());
                                    if (_val == Convert.ToInt32(curValues[dt.TableName + "_Id"]) + 1)
                                    {
                                        curValues[dt.TableName + "_Id"] = curValues[dt.TableName + "_Id"] + 1;
                                    }
                                }
                            }
                            else
                            {
                                foreach (DataColumn dc in dt.Columns)
                                {
                                    if (dc.ColumnName.EndsWith("_Id"))
                                    {
                                        if (!curValues.ContainsKey(dc.ColumnName))
                                        {
                                            curValues.Add(dc.ColumnName, 0);
                                        }
                                        else
                                        {
                                            bool addRow = true;
                                            foreach (DataColumn _c in dt.Columns)
                                            {
                                                if (!_c.ColumnName.EndsWith("_Id"))
                                                {
                                                    if (string.IsNullOrEmpty(dt.Rows[dt.Rows.Count - 1][_c.ColumnName].ToString()))
                                                    {
                                                        addRow = false;
                                                        break;
                                                    }
                                                }
                                            }
                                            if (addRow)
                                            {
                                                curValues[dc.ColumnName] = curValues[dc.ColumnName] + 1;
                                            }
                                            try
                                            {
                                                dt.Rows[dt.Rows.Count - 1][dc.ColumnName] = curValues[dc.ColumnName].ToString();
                                            }
                                            catch (System.Data.InvalidConstraintException ex)
                                            {
                                                if (ex.Message.Contains("ForeignKeyConstraint"))
                                                {
                                                    FillIds();
                                                    dt.Rows[dt.Rows.Count - 1][dc.ColumnName] = curValues[dc.ColumnName].ToString();
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                            dt.Rows[dt.Rows.Count - 1][col.ColumnName] = row[col.Caption].ToString();
                        }
                    }
                    else
                    {
                        dt.Rows[dt.Rows.Count - 1][col.ColumnName] = row[col.Caption].ToString();
                    }
                }
                if (found)
                {
                    break;
                }
            }
        }

        public void Dispose()
        {
            if (finalOutputDataSet != null)
            {
                finalOutputDataSet.Dispose();
            }
            if (InputDataSet != null)
            {
                InputDataSet.Dispose();
            }
        }
    }
}
