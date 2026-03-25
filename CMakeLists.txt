# AUTH:DEVNEUROSIM:7A3F9E2B | Legacy/CMakeLists.txt
cmake_minimum_required(VERSION 3.20)
project(NeuroSim VERSION 1.0 LANGUAGES CXX)

set(CMAKE_CXX_STANDARD 17)
set(CMAKE_CXX_STANDARD_REQUIRED ON)
set(CMAKE_RUNTIME_OUTPUT_DIRECTORY ${CMAKE_BINARY_DIR}/bin)

# ── Sub-projects ──────────────────────────────────────────────────────────────
add_subdirectory(NeuroSim.Core)

option(NEUROSIM_BUILD_PYTHON "Build Python pybind11 bindings" ON)
if(NEUROSIM_BUILD_PYTHON)
    find_package(Python COMPONENTS Interpreter Development QUIET)
    if(Python_FOUND)
        add_subdirectory(NeuroSim.Python)
    else()
        message(STATUS "Python not found — skipping NeuroSim.Python")
    endif()
endif()

# ── Install the DLL next to the C# UI build output ────────────────────────────
install(TARGETS NeuroSimCore
    RUNTIME DESTINATION "${CMAKE_SOURCE_DIR}/NeuroSim.UI/bin/Release/net8.0-windows"
    LIBRARY DESTINATION "${CMAKE_SOURCE_DIR}/NeuroSim.UI/bin/Release/net8.0-windows"
)
