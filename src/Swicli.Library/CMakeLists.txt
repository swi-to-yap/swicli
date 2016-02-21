cmake_minimum_required(VERSION 2.8 FATAL_ERROR)
PROJECT ( YAP_SWICLILIB )


set(MODULE_NAME "Swicli.Library")
set(MODULE_PREFIX "Swicli_Library_")

set(YAP_SWICLILIB_SRC

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
Threads.cs
Tracker.cs
TypeSystem.cs
WaitUntilEvent.cs )

FOREACH( file1 ${YAP_SWICLILIB_SRC} )
    SET( arguments ${arguments} ${file1} )
ENDFOREACH( file1 )

# execute_process(COMMAND rm ${CMAKE_CURRENT_BINARY_DIR}/Swicli.Library.dll )

# https://cmake.org/pipermail/cmake/2007-March/013419.html
add_custom_target(RECOMIPLE_YAP_SWICLILIB ALL dmcs -unsafe -warn:1 -define:PROLOG_YAP /out:${CMAKE_CURRENT_BINARY_DIR}/Swicli.Library.dll ${arguments}
	WORKING_DIRECTORY ${PROJECT_SOURCE_DIR}
        COMMENT add_custom_target dmcs -unsafe -warn:1 -define:PROLOG_YAP /out:${CMAKE_CURRENT_BINARY_DIR}/Swicli.Library.dll ${arguments}
	SOURCES ${arguments}
)
# add_custom_target(YAP_SWICLILIB ALL DEPENDS ${CMAKE_CURRENT_BINARY_DIR}/Swicli.Library.dll)
# else do 
# add_custom_target(YAP_SWICLILIB ALL )



# if(NOT dlls) set(dlls /tmp) endif()

install (FILES ${CMAKE_CURRENT_BINARY_DIR}/Swicli.Library.dll DESTINATION ${dlls} )
