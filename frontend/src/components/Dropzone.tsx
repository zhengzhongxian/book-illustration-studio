import React, { useRef, useState } from 'react';

interface DropzoneProps {
  fileName: string | null;
  onFileLoaded: (fileName: string, content: string) => void;
}

export const Dropzone: React.FC<DropzoneProps> = ({ fileName, onFileLoaded }) => {
  const fileInputRef = useRef<HTMLInputElement>(null);
  const [isDragOver, setIsDragOver] = useState(false);

  const processFile = (file: File) => {
    if (!file.name.endsWith('.txt')) {
      alert('Please upload a plain text (.txt) file.');
      return;
    }

    const reader = new FileReader();
    reader.onload = (e) => {
      const text = e.target?.result as string;
      onFileLoaded(file.name, text);
    };
    reader.readAsText(file);
  };

  const handleDragOver = (e: React.DragEvent) => {
    e.preventDefault();
    setIsDragOver(true);
  };

  const handleDragLeave = () => {
    setIsDragOver(false);
  };

  const handleDrop = (e: React.DragEvent) => {
    e.preventDefault();
    setIsDragOver(false);
    const file = e.dataTransfer.files[0];
    if (file) processFile(file);
  };

  return (
    <div
      className={`dropzone ${fileName ? 'has-file' : ''} ${isDragOver ? 'drag-over' : ''}`}
      tabIndex={0}
      role="button"
      onClick={() => fileInputRef.current?.click()}
      onDragOver={handleDragOver}
      onDragLeave={handleDragLeave}
      onDrop={handleDrop}
      onKeyDown={(e) => {
        if (e.key === 'Enter' || e.key === ' ') {
          e.preventDefault();
          fileInputRef.current?.click();
        }
      }}
    >
      <input
        ref={fileInputRef}
        type="file"
        accept=".txt"
        style={{ display: 'none' }}
        onChange={(e) => {
          const file = e.target.files?.[0];
          if (file) processFile(file);
        }}
      />
      <div style={{ fontSize: '13px', fontWeight: 600, color: 'var(--grad-ink)' }}>
        {fileName ? `✓ ${fileName} loaded` : 'Click or drop a .txt file here'}
      </div>
      <div className="hint">Plain text only · used once as context for every step</div>
    </div>
  );
};
