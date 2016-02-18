cmake_minimum_required(VERSION 2.8 FATAL_ERROR)

enable_language(CSharp)

set(CMAKE_MODULE_PATH ${PROJECT_SOURCE_DIR}/src/cmake/Modules)
enable_testing()
include(${PROJECT_SOURCE_DIR}/src/cmake/Modules/UseGitVersion.cmake)
include(${PROJECT_SOURCE_DIR}/src/cmake/InternalUtils.cmake)
config_compiler_and_linker()

find_package(Mono REQUIRED)

include_directories (
	${MONO_INCLUDE_DIRS}
	)

set(MODULE_NAME "Swicli.Library")
set(MODULE_PREFIX "SWICLI_LIBRARY")

set(${MODULE_PREFIX}_SRCS

Arrays.cs
AssemblyLoader.cs
ClassBuilder.cs
Coercion.cs
CoercionLayout.cs
Collections.cs
DelegateObjectInProlog.cs
DesignByContract.cs
EventHandlerInProlog.cs
Exceptions.cs
ForeignMethod.cs
GenericDelegate.cs
GetSet.cs
IKVMClassLoader.cs
Init.cs
Instance.cs
Invoke.cs
LibPl.cs
LockInfo.cs
NativeMethods.cs
NondetContextHandle.cs
PInvokeByEmit.cs
PInvokeDLL.cs
PInvokeMetaObject.cs
PrologBackedDictionary.cs
PrologClient.cs
PrologCLR.cs
PrologCLRExt.cs
RuntimeTypeInfo.cs
SWI-cs.cs
SWI-cs-PlQuery.cs
SwiPlCs.csproj
Threads.cs
Tracker.cs
TypeSystem.cs
WaitUntilEvent.cs )

add_library(${MODULE_NAME} ${${MODULE_PREFIX}_SRCS})

set_target_properties(${MODULE_NAME} PROPERTIES LINKER_LANGUAGE CSharp)


