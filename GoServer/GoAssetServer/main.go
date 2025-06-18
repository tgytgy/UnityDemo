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

var resourceDir = "D:\\UnityProject\\UnityDemo\\ServerData"

func loggingMiddleware(next http.Handler) http.Handler {
	return http.HandlerFunc(func(w http.ResponseWriter, r *http.Request) {
		startTime := time.Now()
		next.ServeHTTP(w, r)
		log.Printf("%s\t%s\t%s\t%s", r.Method, r.URL.Path, r.RemoteAddr, time.Since(startTime))
	})
}

func buildTargetMiddleware(next http.Handler) http.Handler {
	return http.HandlerFunc(func(w http.ResponseWriter, r *http.Request) {
		// 提取 BuildTarget（如 "/assets/StandaloneWindows64" → "StandaloneWindows64"）
		path := strings.TrimPrefix(r.URL.Path, "/assets/")
		buildTarget := strings.Split(path, "/")[0]

		if buildTarget == "" {
			http.Error(w, "必须指定 BuildTarget（如 /assets/StandaloneWindows64/）", http.StatusBadRequest)
			return
		}

		// 构建完整资源路径
		fullResourceDir := filepath.Join(resourceDir, buildTarget)

		// 检查目录是否存在
		if _, err := os.Stat(fullResourceDir); os.IsNotExist(err) {
			http.Error(w, fmt.Sprintf("目录 %s 不存在", buildTarget), http.StatusNotFound)
			return
		}

		// 创建文件服务器，并移除 "/assets/BuildTarget" 前缀
		fileServer := http.StripPrefix(
			"/assets/"+buildTarget,
			http.FileServer(http.Dir(fullResourceDir)),
		)

		// 处理请求
		fileServer.ServeHTTP(w, r)
	})
}

func main() {
	// 检查资源目录是否存在
	if _, err := os.Stat(resourceDir); os.IsNotExist(err) {
		log.Fatalf("资源目录 %s 不存在", resourceDir)
	}

	// 注册路由，使用中间件链
	http.Handle("/assets/", loggingMiddleware(buildTargetMiddleware(http.HandlerFunc(func(w http.ResponseWriter, r *http.Request) {
		// 此处不会执行，因为 buildTargetMiddleware 已完全处理请求
		http.NotFound(w, r)
	}))))

	port := "8080"
	fmt.Printf("服务器启动，监听端口 %s...\n", port)
	if err := http.ListenAndServe(":"+port, nil); err != nil {
		log.Fatalf("服务器启动失败: %v", err)
	}
}
