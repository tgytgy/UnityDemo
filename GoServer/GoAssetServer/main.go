package main

import (
	"fmt"
	"log"
	"net/http"
	"os"
	"path/filepath"
	"strings"
	"time"
)

// 设置资源文件的根目录
var resourceDir = "/Users/tiangengyu/Desktop/UnityProject/Demo/ServerData/"

// 自定义中间件，用于记录请求日志
func loggingMiddleware(next http.Handler) http.Handler {
	return http.HandlerFunc(func(w http.ResponseWriter, r *http.Request) {
		// 记录请求开始时间
		startTime := time.Now()

		// 调用下一个处理器
		next.ServeHTTP(w, r)

		// 记录请求结束时间并计算耗时
		duration := time.Since(startTime)
		log.Printf(
			"%s\t%s\t%s\t%s",
			r.Method,     // 请求方法
			r.URL.Path,   // 请求路径
			r.RemoteAddr, // 客户端地址
			duration,     // 请求耗时
		)
	})
}

// BuildTarget 处理中间件
func buildTargetMiddleware(next http.Handler) http.Handler {
	return http.HandlerFunc(func(w http.ResponseWriter, r *http.Request) {
		// 提取路径中的 BuildTarget（如 /assets/Windows → "Windows"）
		path := strings.TrimPrefix(r.URL.Path, "/assets/")
		buildTarget := strings.Split(path, "/")[0] // 取第一个部分

		if buildTarget == "" {
			http.Error(w, "BuildTarget is missing in path (e.g., /assets/Windows/)", http.StatusBadRequest)
			return
		}

		// 构建完整资源路径（如 /Desktop/UnityProject/Demo/ServerData/Windows/）
		fullResourceDir := filepath.Join(resourceDir, buildTarget)

		// 检查目录是否存在
		if _, err := os.Stat(fullResourceDir); os.IsNotExist(err) {
			http.Error(w, fmt.Sprintf("BuildTarget directory '%s' does not exist", buildTarget), http.StatusNotFound)
			return
		}

		// 创建文件服务器，并正确处理路径（移除 /assets/BuildTarget/ 前缀）
		fileServer := http.StripPrefix(
			"/assets/"+buildTarget+"/",
			http.FileServer(http.Dir(fullResourceDir)),
		)

		// 处理请求
		fileServer.ServeHTTP(w, r)
	})
}

func main() {
	// 检查资源目录是否存在
	if _, err := os.Stat(resourceDir); os.IsNotExist(err) {
		log.Fatalf("Resource directory '%s' does not exist", resourceDir)
	}

	// 使用 FileServer 处理 /assets/ 路径
	handler := http.HandlerFunc(func(w http.ResponseWriter, r *http.Request) {})
	http.Handle("/assets/", loggingMiddleware(buildTargetMiddleware(handler)))

	// 设置服务器监听地址
	port := "8080"
	fmt.Printf("Starting server on port %s...\n", port)
	if err := http.ListenAndServe(":"+port, nil); err != nil {
		log.Fatalf("Failed to start server: %v", err)
	}
}
