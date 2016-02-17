
#CHECK: Mono

set(CMAKE_CXX_COMPILER_ARG1 "-m64")
set(CMAKE_C_COMPILER_ARG1 "-m64")

find_package(Mono REQUIRED)

add_library (swicliYap64 SHARED swicli64.c)
  
include_directories (${MONO_INCLUDE_DIRS} ${IKVM_INCLUDE_DIRS} )
  
  target_link_libraries(swicliYap64 libYap ${MONO_LIBRARIES} ${IKVM_LIBRARIES})
  
  set_target_properties(swicliYap64 PROPERTIES
    OUTPUT_NAME swicli64 )
  
  # set(YAP_SYSTEM_OPTIONS "swicli " ${YAP_SYSTEM_OPTIONS} PARENT_SCOPE)
  install(TARGETS  swicliYap64
    LIBRARY DESTINATION ${dlls}
		ARCHIVE DESTINATION ${dlls}
  )




