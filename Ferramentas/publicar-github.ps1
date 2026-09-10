<#
  publicar-github.ps1 - publica o projeto Carta Branca no GitHub.

  Uso (PowerShell, na pasta do projeto ou em qualquer lugar):
      .\Ferramentas\publicar-github.ps1
      .\Ferramentas\publicar-github.ps1 -Nome outro-nome -Privado

  Abra o projeto no Unity ANTES de rodar isto: a cena, as animacoes, os prefabs
  e os arquivos .meta so existem depois que o editor importa o projeto, e sem
  os .meta o repositorio abre quebrado na maquina de quem for corrigir.
#>
[CmdletBinding()]
param(
  [string]$Nome = "carta-branca-ultima-mao",
  [string]$Mensagem = "Carta Branca - Ultima Mao: survival game 2D em Unity (Desafio Individual Unity 2)",
  [switch]$Privado
)

$ErrorActionPreference = "Stop"
$raiz = Split-Path -Parent $PSScriptRoot
Set-Location $raiz
Write-Host "Projeto: $raiz" -ForegroundColor Cyan

# --- 1. o Unity ja passou por aqui? ---
$cena = Join-Path $raiz "Assets\Cenas\Arena.unity"
if (-not (Test-Path $cena)) {
  Write-Host ""
  Write-Host "  AVISO: Assets\Cenas\Arena.unity nao existe." -ForegroundColor Yellow
  Write-Host "  O projeto ainda nao foi aberto no Unity, entao a cena, as animacoes," -ForegroundColor Yellow
  Write-Host "  os prefabs e os arquivos .meta nao foram gerados. Se publicar agora," -ForegroundColor Yellow
  Write-Host "  o repositorio vai subir incompleto." -ForegroundColor Yellow
  Write-Host ""
  $r = Read-Host "  Publicar assim mesmo? (s/N)"
  if ($r -ne "s") { Write-Host "Cancelado. Abra a pasta pelo Unity Hub e rode de novo." ; exit 1 }
}

# --- 2. git ---
if (-not (Get-Command git -ErrorAction SilentlyContinue)) {
  throw "git nao encontrado no PATH. Instale o Git para Windows e rode de novo."
}

if (-not (Test-Path (Join-Path $raiz ".git"))) {
  git init -b main | Out-Null
  Write-Host "Repositorio git criado (branch main)." -ForegroundColor Green
} else {
  Write-Host "Repositorio git ja existe aqui." -ForegroundColor DarkGray
}

git add -A
$total = (git diff --cached --name-only | Measure-Object -Line).Lines
if ($total -eq 0) {
  Write-Host "Nada novo para commitar." -ForegroundColor DarkGray
} else {
  git commit -m $Mensagem | Out-Null
  Write-Host "Commit feito: $total arquivos." -ForegroundColor Green
}

# --- 3. GitHub ---
$temRemoto = (git remote) -contains "origin"

if ($temRemoto) {
  Write-Host "Remoto 'origin' ja configurado: $(git remote get-url origin)" -ForegroundColor DarkGray
  git push -u origin main
}
elseif (Get-Command gh -ErrorAction SilentlyContinue) {
  $vis = if ($Privado) { "--private" } else { "--public" }
  Write-Host "Criando repositorio no GitHub via gh ($vis)..." -ForegroundColor Cyan
  gh repo create $Nome $vis --source=. --remote=origin --push
}
else {
  Write-Host ""
  Write-Host "GitHub CLI (gh) nao encontrado. Dois caminhos:" -ForegroundColor Yellow
  Write-Host ""
  Write-Host "  A) instale o gh e rode este script de novo:" -ForegroundColor Yellow
  Write-Host "       winget install GitHub.cli"
  Write-Host "       gh auth login"
  Write-Host ""
  Write-Host "  B) crie o repositorio vazio em github.com/new (nome: $Nome)," -ForegroundColor Yellow
  Write-Host "     sem README nem .gitignore, e rode:" -ForegroundColor Yellow
  Write-Host "       git remote add origin https://github.com/SEU-USUARIO/$Nome.git"
  Write-Host "       git push -u origin main"
  Write-Host ""
  exit 0
}

Write-Host ""
Write-Host "Pronto. Link para colar na atividade:" -ForegroundColor Green
try { Write-Host ("  " + (git remote get-url origin) -replace '\.git$','') -ForegroundColor White } catch {}
