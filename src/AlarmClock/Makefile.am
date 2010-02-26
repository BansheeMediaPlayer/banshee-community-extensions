ASSEMBLY = Banshee.AlarmClock
LINK = $(BANSHEE_LIBS)

SOURCES =  \
	Banshee.AlarmClock/Alarm.cs \
	Banshee.AlarmClock/AlarmClockService.cs \
	Banshee.AlarmClock/AlarmConfigDialog.cs \
	Banshee.AlarmClock/ConfigurationDialog.cs \
	Banshee.AlarmClock/ConfigurationSchema.cs \
	Banshee.AlarmClock/SleepTimerConfigDialog.cs \
	Banshee.AlarmClock/VolumeFade.cs

RESOURCES =  \
	AlarmClock.addin.xml \
	Resources/AlarmMenu.xml

if ENABLE_ALARMCLOCK
include $(top_srcdir)/build/build.mk
else
EXTRA_DIST = $(SOURCES) $(RESOURCES)
endif
