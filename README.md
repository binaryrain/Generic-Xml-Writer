# Generic
Takes data table as input and based on configuration creates XMLs and return list of all XMLs as string.

To Do before using: 
- Configuration file (app or web) should have AppSetting key "FileMappingConfigs" and its value should be directory where configuration file are to be added.
- At 'FileMappingConfigs' folder create two Xml files <processname>.xml and <processname>_outputFormat.xml file. (processname is the second
parameter of WriteRecord method.)
- Inside SampleConfig Files folder I have provided example for both config files.
- NewXMLForEachNewValue node in <processname>.xml : It is comma separated Db Column names based on which new XML will be created.
- DbColumnsThatShouldCombineUnique node in <processname>.xml :  It is comma separated Db Column names based on which XML layering is done. Like
MemberId,PolicyId,BenefitId (meaning a member would have multiple policies each having unique PolicyId for one MemberId, and each policy have multiple benefits
 each having uniue BenefitId for one PolicyId).
 -<processname>_outputFormat.xml : Should have format of XML that you want. Node/attribute Values are to map node name with Db Column name.

How to Use:
1. Call ExtractFileService's class WriteRecord method.
2. To remove empty nodes from returned xmls, for each xml call method removeEmptyNodes(xml)

