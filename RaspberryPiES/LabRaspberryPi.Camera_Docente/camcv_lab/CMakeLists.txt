cmake_minimum_required(VERSION 2.8)
project(camcv_lab)

SET(COMPILE_DEFINITIONS -Werror)

# Per Opencv
find_package(OpenCV REQUIRED)
find_package(Threads)


include_directories(/opt/vc/include)
include_directories(/opt/vc/include/interface/vcos)
include_directories(/opt/vc/include/interface/vcos/pthreads)
include_directories(/opt/vc/include/interface/vmcs_host/linux)
include_directories(.)

add_executable(camcv_lab RaspiCamControl.c RaspiCLI.c RaspiPreview.c student.cpp camcv.cpp)
target_link_libraries(camcv_lab wiringPi /opt/vc/lib/libmmal_core.so /opt/vc/lib/libmmal_util.so /opt/vc/lib/libmmal_vc_client.so /opt/vc/lib/libvcos.so /opt/vc/lib/libbcm_host.so ${OpenCV_LIBS} ${CMAKE_THREAD_LIBS_INIT})

