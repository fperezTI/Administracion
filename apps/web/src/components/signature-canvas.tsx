"use client";

import { useRef, useState } from "react";
import { Button } from "@/components/ui/button";

/** F4's second signature mechanism (see ADR 0006): captures a drawn signature as a PNG data URL via a
 * hidden input, so it travels through a native form submission like every other field in this app —
 * no client-side fetch wiring needed. */
export function SignatureCanvas({ name }: { name: string }) {
  const canvasRef = useRef<HTMLCanvasElement>(null);
  const isDrawing = useRef(false);
  const [dataUrl, setDataUrl] = useState("");

  function getContext() {
    const canvas = canvasRef.current;
    if (!canvas) return null;
    const ctx = canvas.getContext("2d");
    if (!ctx) return null;
    const strokeColor = getComputedStyle(document.documentElement).getPropertyValue("--foreground").trim() || "#171b1f";
    ctx.strokeStyle = strokeColor;
    ctx.lineWidth = 2;
    ctx.lineCap = "round";
    ctx.lineJoin = "round";
    return ctx;
  }

  function getPos(e: React.PointerEvent<HTMLCanvasElement>) {
    const rect = e.currentTarget.getBoundingClientRect();
    return { x: e.clientX - rect.left, y: e.clientY - rect.top };
  }

  function handlePointerDown(e: React.PointerEvent<HTMLCanvasElement>) {
    const ctx = getContext();
    if (!ctx) return;
    isDrawing.current = true;
    const { x, y } = getPos(e);
    ctx.beginPath();
    ctx.moveTo(x, y);
  }

  function handlePointerMove(e: React.PointerEvent<HTMLCanvasElement>) {
    if (!isDrawing.current) return;
    const ctx = getContext();
    if (!ctx) return;
    const { x, y } = getPos(e);
    ctx.lineTo(x, y);
    ctx.stroke();
  }

  function handlePointerUp() {
    if (!isDrawing.current || !canvasRef.current) return;
    isDrawing.current = false;
    setDataUrl(canvasRef.current.toDataURL("image/png"));
  }

  function handleClear() {
    const canvas = canvasRef.current;
    if (!canvas) return;
    canvas.getContext("2d")?.clearRect(0, 0, canvas.width, canvas.height);
    setDataUrl("");
  }

  return (
    <div className="flex flex-col gap-2">
      <canvas
        ref={canvasRef}
        width={400}
        height={140}
        className="bg-background w-full touch-none rounded-lg border"
        onPointerDown={handlePointerDown}
        onPointerMove={handlePointerMove}
        onPointerUp={handlePointerUp}
        onPointerLeave={handlePointerUp}
      />
      <input type="hidden" name={name} value={dataUrl} />
      <div className="flex items-center justify-between">
        <p className="text-muted-foreground text-xs">Dibuja tu firma con el mouse o el dedo.</p>
        <Button type="button" variant="outline" size="sm" onClick={handleClear}>
          Borrar
        </Button>
      </div>
    </div>
  );
}
