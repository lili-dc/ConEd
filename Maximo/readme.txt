Deployment Instruction:

1. Add Maximo table to the existing Maximo MXD document
2. Publish or replace Maximo map service on ArcGIS server
3. In ArcGIS server Rest directory, copy the URL for the Maximo table layer. (i.e. http://host:6080/arcgis/rest/services/maximoundergrd/MapServer/1)
4. In ArcGIS Silverlight Viewer Builder, deploy Maximo.AddIns.xap, and paste the Maximo table URL into the configuration input textbox.  