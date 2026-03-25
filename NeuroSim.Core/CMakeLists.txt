# AUTH:DEVNEUROSIM:7A3F9E2B | Legacy/NeuroSim.Core/CMakeLists.txt
cmake_minimum_required(VERSION 3.20)
project(NeuroSim.Core VERSION 1.0 LANGUAGES CXX)

set(CMAKE_CXX_STANDARD 17)
set(CMAKE_CXX_STANDARD_REQUIRED ON)

# ── Sources ───────────────────────────────────────────────────────────────────
set(SOURCES
    src/genome/BinaryGenome.cpp
    src/genome/RealValuedGenome.cpp
    src/genome/TreeGenome.cpp
    src/genome/GraphGenome.cpp
    src/api/NativeAPI.cpp
)

set(HEADERS
    include/export.h
    include/genome/IGenome.h
    include/genome/BinaryGenome.h
    include/genome/RealValuedGenome.h
    include/genome/TreeGenome.h
    include/genome/GraphGenome.h
    include/api/NativeAPI.h
)

add_library(NeuroSimCore SHARED ${SOURCES} ${HEADERS})

target_include_directories(NeuroSimCore PUBLIC
    $<BUILD_INTERFACE:${CMAKE_CURRENT_SOURCE_DIR}/include>
    $<INSTALL_INTERFACE:include>
)

target_compile_definitions(NeuroSimCore PRIVATE NEUROSIM_CORE_EXPORTS)

if(MSVC)
    target_compile_options(NeuroSimCore PRIVATE /W4 /WX- /MP)
    target_compile_definitions(NeuroSimCore PRIVATE _USE_MATH_DEFINES)
else()
    target_compile_options(NeuroSimCore PRIVATE -Wall -Wextra)
endif()

set_target_properties(NeuroSimCore PROPERTIES
    OUTPUT_NAME "NeuroSimCore"
    RUNTIME_OUTPUT_DIRECTORY "${CMAKE_BINARY_DIR}/bin"
    LIBRARY_OUTPUT_DIRECTORY "${CMAKE_BINARY_DIR}/bin"
)

install(TARGETS NeuroSimCore
    RUNTIME DESTINATION bin
    LIBRARY DESTINATION lib
)
install(DIRECTORY include/ DESTINATION include)
