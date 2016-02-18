cmake_minimum_required(VERSION 2.8 FATAL_ERROR)
PROJECT ( YAP_SWICLILIB )

set(CMAKE_MODULE_PATH ${PROJECT_SOURCE_DIR}/cmake)
# set(CMAKE_MODULE_PATH ${PROJECT_SOURCE_DIR}/cmake/Modules)

find_package(CSharp REQUIRED)


set(CMAKE_CSharp_COMPILER "/usr/bin/dmcs")
set(CMAKE_CSharp_COMPILER_ENV_VAR "CSC")

#enable_testing()
#include(${PROJECT_SOURCE_DIR}/src/cmake/Modules/UseGitVersion.cmake)
#include(${PROJECT_SOURCE_DIR}/src/cmake/InternalUtils.cmake)
#config_compiler_and_linker()


#if(NOT CMAKE_CSharp_CREATE_SHARED_LIBRARY)
#	set(CMAKE_CSharp_CREATE_SHARED_LIBRARY
#		"echo \"dmcs\""
#		"${CMAKE_CSharp_ECHO_ALL}"
#		"${CMAKE_CSharp_COMPILER} /target:library <OBJECTS> /out:<TARGET>")
# endif()


# find_package(Mono REQUIRED)

# include_directories ( ${MONO_INCLUDE_DIRS} )

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

#enable_language(CSharp)

set_target_properties(${MODULE_NAME} PROPERTIES LINKER_LANGUAGE CSharp)

set(CMAKE_CSharp_CREATE_SHARED_LIBRARY "")

# set the final binary dir
set(PROJECT_BINARY_DIR ${PROJECT_SOURCE_DIR}/bin)

# get name and location of binary, namecan change with os (cf. ".exe" suffix on windows)
GET_TARGET_PROPERTY(EXAMPLE_BIN_NAME "YAP_SWICLILIB.h" LOCATION)
# copy bin to binary folder
ADD_CUSTOM_COMMAND(TARGET "YAP_SWICLILIB.h"
          POST_BUILD
          COMMAND ${CMAKE_COMMAND} -E make_directory ${PROJECT_BINARY_DIR}
          COMMAND ${CMAKE_COMMAND} -E copy ${EXAMPLE_BIN_NAME} ${PROJECT_BINARY_DIR}/.
)
