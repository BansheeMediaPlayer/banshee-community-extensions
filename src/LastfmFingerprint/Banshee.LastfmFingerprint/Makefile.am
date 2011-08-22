ASSEMBLY = Banshee.LastfmFingerprint
DLLCONFIG = $(ASSEMBLY).dll.config
LINK = $(BANSHEE_LIBS) $(BANSHEE_LASTFM_LIBS)

SOURCES =  \
	AudioDecoder.cs \
	FingerprintRequest.cs \
	LastfmAccount.cs \
	LastfmFingerprintService.cs \
	LoginDialog.cs \
	LoginForm.cs \
	VerifyUserRequest.cs

RESOURCES =  \
	LastfmFingerprint.addin.xml \
	Resources/GlobalUI.xml

if ENABLE_LASTFMFINGERPRINT
include $(top_srcdir)/build/build.mk
EXTRA_DIST += $(DLLCONFIG)
module_DATA = $(DLLCONFIG)
else
EXTRA_DIST = $(SOURCES) $(RESOURCES) $(DLLCONFIG)
endif
