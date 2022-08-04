MODNAME		:= Shabby
KSPDIR		:= ${HOME}/ksp/KSP_linux
MANAGED		:= ${KSPDIR}/KSP_Data/Managed
GAMEDATA	:= ${KSPDIR}/GameData
MODGAMEDATA := ${GAMEDATA}/${MODNAME}
PLUGINDIR	:= ${MODGAMEDATA}/Plugins

RESGEN2		:= resgen2
GMCS		:= gmcs
GMCSFLAGS	:= -optimize -warnaserror
GIT			:= git
TAR			:= tar
ZIP			:= zip

all:

info:
	@echo "Shabby Build Information"
	@echo "    resgen2:    ${RESGEN2}"
	@echo "    gmcs:       ${GMCS}"
	@echo "    gmcs flags: ${GMCSFLAGS}"
	@echo "    git:        ${GIT}"
	@echo "    tar:        ${TAR}"
	@echo "    zip:        ${ZIP}"
	@echo "    KSP Data:   ${KSPDIR}"

HARMONY := \
	0Harmony.dll \
	Harmony.version \
	HarmonyInstallChecker.dll \
	ReadMe.txt

HARMONYDIR := ${GAMEDATA}/000_Harmony
clean:
	#rm -f icon_button.png

install: all
	mkdir -p ${HARMONYDIR}
	cp ${HARMONY} ${HARMONYDIR}

.PHONY: all clean install
