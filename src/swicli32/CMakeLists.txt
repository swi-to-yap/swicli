
#CHECK: Mono

set(CMAKE_CXX_COMPILER_ARG1 "-m32")
set(CMAKE_C_COMPILER_ARG1 "-m32")

find_package(Mono REQUIRED)

add_library (swicliYap32 SHARED swicli32.c)
  
include_directories( 
		${MONO_INCLUDE_DIRS} ${IKVM_INCLUDE_DIRS} )
  
target_link_libraries(swicliYap32 libYap ${MONO_LIBRARIES} ${IKVM_LIBRARIES})
  
set_target_properties(swicliYap32 
 PROPERTIES
    OUTPUT_NAME swicliYap32 )
  
  # set(YAP_SYSTEM_OPTIONS "SWICLIYAP32 " ${YAP_SYSTEM_OPTIONS} PARENT_SCOPE)
  install(TARGETS  swicliYap32
    LIBRARY DESTINATION ${dlls}
		ARCHIVE DESTINATION ${dlls}
  )



