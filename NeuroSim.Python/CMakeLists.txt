# AUTH:DEVNEUROSIM:7A3F9E2B | Legacy/NeuroSim.Python/CMakeLists.txt
cmake_minimum_required(VERSION 3.20)
project(neurosim_python LANGUAGES CXX)

set(CMAKE_CXX_STANDARD 17)

# Fetch pybind11 if not found
find_package(pybind11 QUIET)
if(NOT pybind11_FOUND)
    include(FetchContent)
    FetchContent_Declare(pybind11
        GIT_REPOSITORY https://github.com/pybind/pybind11.git
        GIT_TAG        v2.12.0)
    FetchContent_MakeAvailable(pybind11)
endif()

find_package(Python COMPONENTS Interpreter Development REQUIRED)

pybind11_add_module(neurosim_core bindings.cpp
    ../NeuroSim.Core/src/genome/BinaryGenome.cpp
    ../NeuroSim.Core/src/genome/RealValuedGenome.cpp
    ../NeuroSim.Core/src/genome/TreeGenome.cpp
    ../NeuroSim.Core/src/genome/GraphGenome.cpp
    ../NeuroSim.Core/src/api/NativeAPI.cpp
)

target_include_directories(neurosim_core PRIVATE
    ../NeuroSim.Core/include
)

target_compile_definitions(neurosim_core PRIVATE
    NEUROSIM_CORE_EXPORTS
    _USE_MATH_DEFINES
)

if(MSVC)
    target_compile_options(neurosim_core PRIVATE /MP /W3)
endif()
