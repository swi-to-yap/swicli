cmake_minimum_required (VERSION 2.8)

project(swicli)

set(CMAKE_LOCAL_ROOT ${CMAKE_SOURCE_DIR}/cmake)
set(CMAKE_MODULE_PATH ${CMAKE_MODULE_PATH} "${CMAKE_LOCAL_ROOT}/Modules")

set(EXECUTABLE_OUTPUT_PATH ${CMAKE_CURRENT_BINARY_DIR}/bin)
set(LIBRARY_OUTPUT_PATH ${CMAKE_CURRENT_BINARY_DIR}/bin)

enable_language(CSharp)

# cmake_minimum_required(VERSION 3.2)
# project(InterOp)

# TODO: https://help.github.com/articles/creating-project-pages-manually/

# TODO: Handle Windows Shared libs: https://cmake.org/Wiki/BuildingWinDLL


	# add_subdirectory (src/swicli32)

	add_subdirectory (src/swicli64)

  # add_subdirectory (src/Swicli.Library)  

	install(FILES prolog/swicli.pl prolog/swicffi.pl
			DESTINATION ${libpl}
    )

