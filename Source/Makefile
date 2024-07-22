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
	Shabby.cs					\
	ShabLoader.cs				\
	MaterialDef.cs				\
	MaterialReplacements.cs		\
	$e

RESGEN2		:= resgen2
CSC			:= csc
CSCFLAGS	:= -optimize -warnaserror -debug -unsafe
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
	@echo "    csc:        ${CSC}"
	@echo "    csc flags:  ${CSCFLAGS}"
	@echo "    git:        ${GIT}"
	@echo "    tar:        ${TAR}"
	@echo "    zip:        ${ZIP}"
	@echo "    KSP Data:   ${KSPDIR}"

bin/Shabby.dll: ${Shabby_FILES}
	@mkdir -p bin
	${CSC} ${CSCFLAGS} -highentropyva- -noconfig -nostdlib+ -t:library \
		-lib:${MANAGED} \
		-r:${MANAGED}/mscorlib.dll,${MANAGED}/System.dll,${MANAGED}/System.Core.dll \
		-r:Assembly-CSharp.dll,Assembly-CSharp-firstpass.dll \
		-r:UnityEngine.dll \
		-r:UnityEngine.CoreModule.dll \
		-r:UnityEngine.AssetBundleModule.dll \
		-r:../Assets/0Harmony.dll \
		-out:$@ $^

clean:
	rm -f ${TARGETS}* assembly/AssemblyInfo.cs bin/${MODNAME}.version
	test -d bin && rmdir bin || true

install: all
	mkdir -p ${PLUGINDIR}
	cp ${TARGETS} bin/${MODNAME}.version ${PLUGINDIR}

.PHONY: all clean install
