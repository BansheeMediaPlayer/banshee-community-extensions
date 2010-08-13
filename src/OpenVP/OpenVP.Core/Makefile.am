ASSEMBLY = OpenVP.Core
LINK =  -r:System $(TAO_OPENGL_LIBS) $(TAO_FREEGLUT_LIBS) \
	-r:$(DIR_BIN)/Cdh.Affe.dll -r:$(DIR_BIN)/OpenVP.dll -r:$(DIR_BIN)/OpenVP.Scripting.dll

SOURCES =  \
	Buffer.cs \
	BurstScope.cs \
	ClearScreen.cs \
	DynamicMovement.cs \
	Laser.cs \
	Mirror.cs \
	MovementBase.cs \
	Scope.cs \
	ScopeBase.cs \
	SharedTextureHandle.cs \
	SuperScope.cs \
	TextureHandle.cs

RESOURCES =  

if ENABLE_OPENVP
include $(top_srcdir)/build/build.mk
else
EXTRA_DIST = $(SOURCES) $(RESOURCES)
endif
