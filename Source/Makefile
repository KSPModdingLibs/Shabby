export MODNAME		:= Shabby
export KSPDIR		:= ${HOME}/ksp/KSP_linux
export MANAGED		:= ${KSPDIR}/KSP_Data/Managed
export GAMEDATA		:= ${KSPDIR}/GameData
export MODGAMEDATA	:= ${GAMEDATA}/${MODNAME}
export PLUGINDIR	:= ${MODGAMEDATA}/Plugins

TARGETS		:= bin/Shabby.dll

Shabby_FILES := \
	assembly/AssemblyInfo.cs	\
	assembly/VersionReport.cs	\
	Detourer.cs					\
	MaterialDetour.cs			\
	ShabLoader.cs				\
	$e

RESGEN2		:= resgen2
GMCS		:= mcs
GMCSFLAGS	:= -optimize -warnaserror -debug -unsafe
GIT			:= git
TAR			:= tar
ZIP			:= zip

all: version ${TARGETS}

.PHONY: version
version:
	@../tools/git-version.sh

info:
	@echo "${MODNAME} Build Information"
	@echo "    resgen2:    ${RESGEN2}"
	@echo "    gmcs:       ${GMCS}"
	@echo "    gmcs flags: ${GMCSFLAGS}"
	@echo "    git:        ${GIT}"
	@echo "    tar:        ${TAR}"
	@echo "    zip:        ${ZIP}"
	@echo "    KSP Data:   ${KSPDIR}"

bin/Shabby.dll: ${Shabby_FILES}
	@mkdir -p bin
	${GMCS} ${GMCSFLAGS} -t:library -lib:${MANAGED} \
		-r:Assembly-CSharp,Assembly-CSharp-firstpass \
		-r:UnityEngine \
		-r:UnityEngine.CoreModule \
		-r:UnityEngine.AssetBundleModule \
		-out:$@ $^

clean:
	rm -f ${TARGETS}* assembly/AssemblyInfo.cs bin/${MODNAME}.version
	test -d bin && rmdir bin || true

install: all
	mkdir -p ${PLUGINDIR}
	cp ${TARGETS} bin/${MODNAME}.version ${PLUGINDIR}

.PHONY: all clean install
