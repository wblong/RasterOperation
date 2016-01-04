using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.DataSourcesRaster;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geometry;

using ESRI.ArcGIS.Carto;
using ESRI.ArcGIS.Display;

namespace ConsoleRaster
{
    class Program
    {
        static void Main(string[] args)
        {
            //运行环境初始化
            ESRI.ArcGIS.RuntimeManager.Bind(ESRI.ArcGIS.ProductCode.EngineOrDesktop);
            IAoInitialize aoinitialize = new AoInitializeClass();
            esriLicenseStatus status = aoinitialize.Initialize(esriLicenseProductCode.esriLicenseProductCodeStandard);

            string path = System.Environment.CurrentDirectory;
            string name = "result.tif";


            IWorkspaceFactory workspaceFactory = new RasterWorkspaceFactoryClass();
            IRasterWorkspace rasterWorkspace = (IRasterWorkspace)(workspaceFactory.OpenFromFile(path, 0));
            IRasterDataset pRasterDataset = rasterWorkspace.OpenRasterDataset(name);
            //创建栅格瓦片
            CreateTilesFromRasterDataset(pRasterDataset, rasterWorkspace as IWorkspace, 500, 500);
            //修改栅格的值
            ChangeRasterValue(pRasterDataset as IRasterDataset2);
            aoinitialize.Shutdown();

        }

        

#region "Open Raster File As GeoDatset"

/// <summary>
/// Open a Raster file on disk by it's name as a GeoDataset.
/// </summary>
/// <param name="path">A System.String that is the directory location of the raster file. Example: "C:\raster_data"</param>
/// <param name="name">A System.String that is the name of the raster file in the directory. Example: "landuse" or "watershed"</param>
/// <returns>An IGeoDataset interface.</returns>
/// <remarks>
/// IRasterWorkspace is used to access a raster stored in a file system in any supported raster format. 
/// RasterWorkspaceFactory must be used to create a raster workspace.
/// To access raster from geodatabase, use IRasterWorkspaceEx interface.
/// 
/// For more information on working with the ArcGIS Spatial Anaylst objects see:
/// http://edndoc.esri.com/arcobjects/9.2/CPP_VB6_VBA_VCPP_Doc/COM/VB6/working/work_rasters/sptl_analyst_objs.htm
/// </remarks>
public static IGeoDataset OpenRasterFileAsGeoDatset(String path, String name)
{

    try
    {
        //打开栅格图像
        IWorkspaceFactory workspaceFactory = new RasterWorkspaceFactoryClass();
        IRasterWorkspace rasterWorkspace = (IRasterWorkspace)(workspaceFactory.OpenFromFile(path, 0));
        IRasterDataset rasterDataset = rasterWorkspace.OpenRasterDataset(name);
        IGeoDataset geoDataset = (IGeoDataset)rasterDataset; // Explicit Cast

        return geoDataset;

    }
    catch (Exception ex)
    {

        //Diagnostics.Debug.WriteLine(ex.Message)
        return null;

    }

}
#endregion
        /// <summary>
        /// 栅格操作修改栅格的值
        /// </summary>
        /// <param name="pRasterDataset2"></param>
public static void ChangeRasterValue(IRasterDataset2 pRasterDataset2)
{
    //设置读取栅格的大小
    IRaster2 pRaster2 = pRasterDataset2.CreateFullRaster() as IRaster2;
    IPnt pPntBlock = new PntClass();
    pPntBlock.X = 128;
    pPntBlock.Y = 128;
    IRasterCursor pRasterCursor = pRaster2.CreateCursorEx(pPntBlock);
    IRasterEdit pRasterEdit = pRaster2 as IRasterEdit;

    if (pRasterEdit.CanEdit())
    {
        //循环波段,长和宽度
        IRasterBandCollection pBands = pRasterDataset2 as IRasterBandCollection;
        IPixelBlock3 pPixelblock3 = null;
        int pBlockwidth = 0;
        int pBlockheight = 0;
        System.Array pixels;
        object pValue;
        long pBandCount = pBands.Count;
        //
        IRasterProps pRasterProps = pRaster2 as IRasterProps;
        object nodata = pRasterProps.NoDataValue;
        do
        {
            pPixelblock3 = pRasterCursor.PixelBlock as IPixelBlock3;
            pBlockwidth = pPixelblock3.Width;
            pBlockheight = pPixelblock3.Height;
            for (int k = 0; k < pBandCount; k++)
            {
                pixels = (System.Array)pPixelblock3.get_PixelData(k);
                for (int i = 0; i < pBlockwidth; i++)
                {
                    for (int j = 0; j < pBlockheight; j++)
                    {
                        pValue = pixels.GetValue(i, j);
                        int value = Convert.ToInt32(pValue);
                        if (Convert.ToInt32(pValue) != 3)
                        {
                            pixels.SetValue(Convert.ToByte(0), i, j);
                        }
                    }
                }
                pPixelblock3.set_PixelData(k, pixels);
            }
            pPntBlock = pRasterCursor.TopLeft;
            pRasterEdit.Write(pPntBlock, (IPixelBlock)pPixelblock3);

        } while (pRasterCursor.Next());
        System.Runtime.InteropServices.Marshal.ReleaseComObject(pRasterEdit);

    }

}
        /// <summary>
        /// 栅格分块
        /// </summary>
        /// <param name="pRasterDataset"></param>
        /// <param name="pOutputWorkspace"></param>
        /// <param name="pWidth"></param>
        /// <param name="pHeight"></param>
public static void CreateTilesFromRasterDataset(IRasterDataset pRasterDataset, IWorkspace pOutputWorkspace, int pWidth, int pHeight)
{

    IRasterProps pRasterProps = (IRasterProps)pRasterDataset.CreateDefaultRaster();// cast IRaster to IRasterProps
    double xTileSize = pRasterProps.MeanCellSize().X * pWidth;
    double yTileSize = pRasterProps.MeanCellSize().Y * pHeight;


    int xTileCount = (int)Math.Ceiling((double)pRasterProps.Width / pWidth);
    int yTileCount = (int)Math.Ceiling((double)pRasterProps.Height / pHeight);

    IEnvelope pExtent = pRasterProps.Extent;
    IEnvelope pTileExtent = new EnvelopeClass();
    ISaveAs pSaveAs = null;

    for (int i = 0; i < xTileCount; i++)
    {
        for (int j = 0; j < yTileCount; j++)
        {
            pRasterProps = (IRasterProps)pRasterDataset.CreateDefaultRaster();

            pTileExtent.XMin = pExtent.XMin + i * xTileSize;
            pTileExtent.XMax = pTileExtent.XMin + xTileSize;
            pTileExtent.YMin = pExtent.YMin + j * yTileSize;
            pTileExtent.YMax = pTileExtent.YMin + yTileSize;


            pRasterProps.Height = pHeight;
            pRasterProps.Width = pWidth;

            pRasterProps.Extent = pTileExtent;
            pSaveAs = (ISaveAs)pRasterProps;
            pSaveAs.SaveAs("tile_" + i + "_" + j + ".tif", pOutputWorkspace, "TIFF");


        }
    }


}
        /// <summary>
        /// 波段计算和金字塔构建
        /// </summary>
        /// <param name="pRasterDataset"></param>
        public static void CalculateStatsAndPyramids(IRasterDataset pRasterDataset){

            IRasterBandCollection pBandColl=(IRasterBandCollection)pRasterDataset;
            //波段统计
            for (int i=0;i<pBandColl.Count;i++)
            {

                IRasterBand pRasterBand=pBandColl.Item(i);
                pRasterBand.ComputeStatsAndHist();
                //IRasterStatistics pRasterStatistics= pRasterBand.Statistics;
                //double Maximum=pRasterStatistics.Maximum;
                //IRasterHistogram pRasterHistogram=pRasterBand.Histogram;
               
            }
            IRasterPyramid pRasterPyramids=(IRasterPyramid)pRasterDataset;
            if (pRasterPyramids.Present==false)
            {
                pRasterPyramids.Create();
            }
        }

        public static IRasterRenderer ClassifyRenderer(ESRI.ArcGIS.Geodatabase.IRasterDataset pRasterDataset){

            try
            {
                //create the classify render:创建渲染器
                IRasterClassifyColorRampRenderer pClassifyRenderer=new RasterClassifyColorRampRendererClass();
                IRasterRenderer pRasterRenderer=(IRasterRenderer)pClassifyRenderer;
                IRaster pRaster=pRasterDataset.CreateDefaultRaster();
                pRasterRenderer.Raster=pRaster;
                pClassifyRenderer.ClassCount=10;
                pRasterRenderer.Update();
                
                //创建颜色
                IRgbColor pFromColor=new RgbColorClass();
                pFromColor.Red=255;
                pFromColor.Green=0;
                pFromColor.Blue=0;
                IRgbColor pToColor=new RgbColorClass();
                pToColor.Red=0;
                pToColor.Green=255;
                pToColor.Blue=255;

                //set the color ramp for the symbology:生成色带
                IAlgorithmicColorRamp pRamp=new AlgorithmicColorRampClass();
                pRamp.Size=10;
                pRamp.FromColor=pFromColor;
                pRamp.ToColor=pToColor;
                bool pBoolColorRamp;
                pRamp.CreateRamp(out pBoolColorRamp);
                //create the symbol for the classes.:创建符号
                IFillSymbol pFillSymbol=new SimpleFillSymbolClass();
                for (int i=0;i<pClassifyRenderer.ClassCount;i++){

                    pFillSymbol.Color=pRamp.get_Color(i);
                    pClassifyRenderer.set_Symbol(i,(ISymbol)pFillSymbol);
                    pClassifyRenderer.set_Label(i,Convert.ToString(i));

                }
                return pRasterRenderer;

            }
            catch (System.Exception ex)
            {
            	System.Diagnostics.Debug.WriteLine(ex.Message);
                return null;
            }
        }
        public static IRasterRenderer StretchRender(IRasterDataset pRasterDataset){


            try
            {
                //Define the from and to colors for the color ramp
                IRgbColor pFromColor=new RgbColorClass();
                pFromColor.Red=255;
                pFromColor.Green=0;
                pFromColor.Blue=0;
                IRgbColor pToColor=new RgbColorClass();
                pToColor.Red=0;
                pToColor.Green=255;
                pToColor.Blue=0;

                IAlgorithmicColorRamp pRamp=new AlgorithmicColorRampClass();
                pRamp.Size=10;
                pRamp.FromColor=pFromColor;
                pRamp.ToColor=pToColor;
                bool createColorRamp;
                pRamp.CreateRamp(out createColorRamp);
                //create a stretch renderer
                IRasterStretchColorRampRenderer pStrechRenderer=new RasterStretchColorRampRendererClass();
                IRasterRenderer pRasterRenderer=(IRasterRenderer)pStrechRenderer;
                //set the renderer properties
                IRaster pRaster =pRasterDataset.CreateDefaultRaster();
                pRasterRenderer.Raster=pRaster;
                pRasterRenderer.Update();
                //
                pStrechRenderer.BandIndex=0;
                pStrechRenderer.ColorRamp=pRamp;
                IRasterStretch  pStretchType=(IRasterStretch)pRasterRenderer;
                pStretchType.StretchType=esriRasterStretchTypesEnum.esriRasterStretch_StandardDeviations;
                pStretchType.StandardDeviationsParam=2;
                return pRasterRenderer;

            }
            catch (System.Exception ex)
            {
            	System.Diagnostics.Debug.WriteLine(ex.Message);
                return null;
            }
        }
    }
}
