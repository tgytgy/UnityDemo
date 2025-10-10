package main

import (
	"encoding/binary"
	"fmt"
	"io"
	"net"

	"google.golang.org/protobuf/proto"

	// 生成的 Go 包导入路径按你的项目结构来
	// 假设 protoc 生成在 proto/echo.pb.go，包名为 echo
	protoecho "GoGameServer/proto" // <- 替换为你的真实模块路径，如: github.com/you/tcp-proto-demo/proto
)

func readFrame(conn net.Conn) ([]byte, error) {
	var lenBuf [4]byte
	if _, err := io.ReadFull(conn, lenBuf[:]); err != nil {
		return nil, err
	}
	n := binary.BigEndian.Uint32(lenBuf[:])
	if n == 0 || n > 10*1024*1024 {
		return nil, fmt.Errorf("invalid frame size: %d", n)
	}
	buf := make([]byte, n)
	if _, err := io.ReadFull(conn, buf); err != nil {
		return nil, err
	}
	return buf, nil
}

func writeFrame(conn net.Conn, payload []byte) error {
	var lenBuf [4]byte
	binary.BigEndian.PutUint32(lenBuf[:], uint32(len(payload)))
	if _, err := conn.Write(lenBuf[:]); err != nil {
		return err
	}
	_, err := conn.Write(payload)
	return err
}

func handleClient(c net.Conn) {
	defer c.Close()
	for {
		frame, err := readFrame(c)
		if err != nil {
			if err != io.EOF {
				fmt.Println("read error:", err)
			}
			return
		}

		var in protoecho.Chat
		if err := proto.Unmarshal(frame, &in); err != nil {
			fmt.Println("unmarshal error:", err)
			return
		}

		fmt.Printf("收到 Chat: seq=%d, text=%q\n", in.Seq, in.Text)

		// 回 echo
		out := &protoecho.Chat{
			Seq:  in.Seq,
			Text: "已收到: " + in.Text,
		}
		payload, err := proto.Marshal(out)
		if err != nil {
			fmt.Println("marshal error:", err)
			return
		}
		if err := writeFrame(c, payload); err != nil {
			fmt.Println("write error:", err)
			return
		}
	}
}

func main() {
	ln, err := net.Listen("tcp", ":9000")
	if err != nil {
		panic(err)
	}
	fmt.Println("Go Proto Server 监听 :9000")
	for {
		conn, err := ln.Accept()
		if err != nil {
			continue
		}
		go handleClient(conn)
	}
}
