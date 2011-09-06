ASSEMBLY = Banshee.OpenVP.Visualizations
LINK =  -r:System $(TAO_OPENGL_LIBS) \
	-r:$(DIR_BIN)/OpenVP.dll -r:$(DIR_BIN)/OpenVP.Core.dll

SOURCES =  \
	Conway/GenerationalLifeBoard.cs \
	Conway/GenerationalLifeSquare.cs \
	Conway/GenerationalLifeVis.cs \
	Core.cs \
	GlassWall.cs \
	GreenFlower.cs \
	Grid.cs \
	Inferno.cs \
	SimpleScope.cs \
	SpectrumAnalyzer.cs \
	SpectrumScan.cs \
	SpinningGrid.cs \
	Voiceprint.cs

RESOURCES = resources/Banshee.OpenVP.Visualizations.addin.xml

if ENABLE_OPENVP
include $(top_srcdir)/build/build.mk
else
EXTRA_DIST = $(SOURCES) $(RESOURCES)
endif
