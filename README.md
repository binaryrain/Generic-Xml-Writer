# Generic
Takes data table as input and based on configuration creates XMLs and return list of all XMLs as string.

To Do before using: 
- Configuration file (app or web) should have AppSetting key "FileMappingConfigs" and its value should be directory where configuration file are to be added.
- At 'FileMappingConfigs' folder create two Xml files <processname>.xml and <processname>_outputFormat.xml file. (processname is the second parameter of WriteRecord method.)
- Inside SampleConfig Files folder I have provided example for both config files.

-----------------------------------------------------------------------------------------------------------------------------------
- NewXMLForEachNewValue node in <processname>.xml : It is comma separated Db Column names based on which new XML will be created.
- DbColumnForSorting node in <processname>.xml : It is comma separated Db column names to sort the datatable.
- DbColumnsThatShouldCombineUnique node in <processname>.xml :  It is comma separated Db Column names based on which XML layering is done. If there are repeating nodes on same level then we need two layering separated by #.
  
  Example 1 : 
  <items>
	<item id="0001" type="donut"> -- REAPEATING NODE
		<name>Cake</name>
		<ppu>0.55</ppu>
		<batters>
			<batter id="1001">Regular</batter> -- REAPEATING NODE
			<batter id="1002">Chocolate</batter>
			<batter id="1003">Blueberry</batter>
		</batters>
		<topping id="5001">None</topping> -- REAPEATING NODE
		<topping id="5002">Glazed</topping>
		<topping id="5005">Sugar</topping>		
	</item>
	...
</items>
DbColumnsThatShouldCombineUnique would be "ItemId,BatterId#ItemId,ToppingId" (# because BatterId and ToppingId are on same level)

Example 2:
<Root>
	<artists>
		<artist>  -- REPEATING NODE
			<Id>1</Id>
			<Name>assd</Name>
			<Albums>
			    <Album>   -- REPEATING NODE
          			<id>AlbumId</id>
          			<Name>AlbumName</Name>
          			<Genre>Genre</Genre>
				         <Songs>
            				<Song>  -- REPEATING NODE
              				  <Id>SongId</Id>
              				  <Name>SongName</Name>
            				</Song>
       			   </Songs>
			    </Album>
			</Albums>
		</artist>
	</artists>
</Root>

DbColumnsThatShouldCombineUnique would be "ArtistId,AlbumId,SongId" (because each artist has unique album and each album has unique song)
  
 -<processname>_outputFormat.xml : Should have format of XML that you want. Node/attribute Values are to map node name with Db Column name.

-----------------------------------------------------------------------------------------------------------------------------------

How to Use:
1. Call ExtractFileService's class WriteRecord method.
2. To remove empty nodes from returned xmls, for each xml call method removeEmptyNodes(xml)

