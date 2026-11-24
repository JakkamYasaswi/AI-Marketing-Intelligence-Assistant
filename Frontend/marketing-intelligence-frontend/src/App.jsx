
import { useState } from 'react'
import jsPDF from 'jspdf'
import './App.css'

function App() {
  const [url, setUrl] = useState('')
  const [result, setResult] = useState(null)
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState('')

  const handleAudit = async (e) => {
    e.preventDefault()

    setLoading(true)
    setError('')
    setResult(null)

    try {
      const response = await fetch('https://localhost:7082/api/Audit', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify({ url }),
      })

      const data = await response.json()

      if (!response.ok) {
        throw new Error(
          typeof data === 'string'
            ? data
            : data.message || data.title || 'Website audit failed.'
        )
      }

      setResult(data)
    } catch (err) {
      setError(err.message || 'Unable to connect to the backend API.')
    } finally {
      setLoading(false)
    }
  }

  const page = result?.pages?.[0]

  // Download SEO Audit Report as PDF
  const downloadPDF = () => {
    if (!result) return

    const doc = new jsPDF()
    let y = 20

    const addText = (text, fontSize = 11) => {
      doc.setFontSize(fontSize)

      const lines = doc.splitTextToSize(String(text ?? ''), 170)
      const requiredHeight = lines.length * 7 + 3

      if (y + requiredHeight > 275) {
        doc.addPage()
        y = 20
      }

      doc.text(lines, 20, y)
      y += requiredHeight
    }

    // Report heading
    doc.setFont('helvetica', 'bold')
    addText('AI Marketing Intelligence', 18)
    addText('SEO Audit Report', 15)

    doc.setFont('helvetica', 'normal')
    addText(`Website: ${result.websiteUrl || url}`)
    addText(`Overall SEO Score: ${result.overallSeoScore ?? 0}/100`)
    addText(`Pages Crawled: ${result.totalPagesCrawled ?? 0}`)

    // SEO Metrics
    addText('SEO Metrics', 15)

    page?.seoMetrics?.forEach((metric, index) => {
      addText(`${index + 1}. ${metric.metricName}`, 12)
      addText(`Status: ${metric.status}`)
      addText(`Score: ${metric.score} Points`)
      addText(`Details: ${metric.details}`)
    })

    // Page Details
    addText('Page Details', 15)
    addText(`Website URL: ${page?.url || 'N/A'}`)
    addText(`Page Title: ${page?.pageTitle || 'Missing'}`)
    addText(`Title Length: ${page?.titleLength ?? 0} characters`)
    addText(`Meta Description: ${page?.metaDescription || 'Missing'}`)
    addText(`Meta Description Length: ${page?.metaDescriptionLength ?? 0} characters`)
    addText(`H1 Count: ${page?.h1Count ?? 0}`)
    addText(`Canonical URL: ${page?.canonicalUrl || 'Missing'}`)
    addText(`JSON-LD Count: ${page?.jsonLdCount ?? 0}`)

    // Broken Links
    addText('Broken Internal Links', 15)
    addText(`Total Broken Links: ${page?.brokenLinksCount ?? 0}`)

    if (page?.brokenLinks?.length > 0) {
      page.brokenLinks.forEach((link, index) => {
        addText(`Broken Link ${index + 1}`, 12)
        addText(`URL: ${link.url}`)
        addText(`Found On: ${link.foundOnPage}`)
        addText(`Status Code: ${link.statusCode}`)
      })
    } else {
      addText('No broken internal links found.')
    }

    // Gemini Recommendations
    addText('Gemini AI Recommendations', 15)
    addText(result.recommendations || 'No recommendations available.')

    // Save PDF
    doc.save('SEO-Audit-Report.pdf')
  }

  return (
    <div className="app">
      <header className="header">
        <h2>AI Marketing Intelligence</h2>
        <span>SEO Audit Assistant</span>
      </header>

      <main className="container">
        <section className="hero">
          <h1>Analyze Your Website SEO</h1>

          <p>
            Discover SEO issues and get AI-powered recommendations
            to improve your website performance.
          </p>

          <form onSubmit={handleAudit} className="audit-form">
            <input
              type="url"
              placeholder="Enter website URL (https://example.com)"
              value={url}
              onChange={(e) => setUrl(e.target.value)}
              required
            />

            <button type="submit" disabled={loading}>
              {loading ? 'Analyzing...' : 'Analyze Website'}
            </button>
          </form>
        </section>

        {error && <div className="error">{error}</div>}

        {loading && (
          <p className="loading">
            Analyzing your website. Please wait...
          </p>
        )}

        {result && (
          <section className="results">
            <div className="section-heading">
              <h2>SEO Audit Results</h2>
              <p>{result.websiteUrl}</p>
            </div>

            <button
              type="button"
              className="download-btn"
              onClick={downloadPDF}
            >
              Download PDF Report
            </button>

            <div className="summary">
              <div className="result-card">
                <h3>Overall SEO Score</h3>
                <p className="score">
                  {result.overallSeoScore ?? 0}
                  <small> / 100</small>
                </p>
              </div>

              <div className="result-card">
                <h3>Pages Crawled</h3>
                <p className="score">
                  {result.totalPagesCrawled ?? 0}
                </p>
              </div>
            </div>

            <h3 className="sub-heading">SEO Metrics</h3>

            <div className="metrics-grid">
              {page?.seoMetrics?.map((metric, index) => (
                <div className="metric-card" key={index}>
                  <div className="metric-header">
                    <h3>{metric.metricName}</h3>

                    <span
                      className={
                        metric.status?.toLowerCase() === 'pass'
                          ? 'status pass'
                          : 'status fail'
                      }
                    >
                      {metric.status}
                    </span>
                  </div>

                  <p className="metric-score">
                    {metric.score} Points
                  </p>

                  <p className="metric-details">
                    {metric.details}
                  </p>
                </div>
              ))}
            </div>

            <h3 className="sub-heading">Page Details</h3>

            <div className="details-card">
              <p>
                <strong>Website URL:</strong> {page?.url}
              </p>

              <p>
                <strong>Page Title:</strong> {page?.pageTitle || 'Missing'}
              </p>

              <p>
                <strong>Title Length:</strong> {page?.titleLength ?? 0} characters
              </p>

              <p>
                <strong>Meta Description:</strong>{' '}
                {page?.metaDescription || 'Missing'}
              </p>

              <p>
                <strong>Meta Description Length:</strong>{' '}
                {page?.metaDescriptionLength ?? 0} characters
              </p>

              <p>
                <strong>H1 Count:</strong> {page?.h1Count ?? 0}
              </p>

              <p>
                <strong>Canonical URL:</strong>{' '}
                {page?.canonicalUrl || 'Missing'}
              </p>

              <p>
                <strong>JSON-LD Count:</strong> {page?.jsonLdCount ?? 0}
              </p>
            </div>

            <h3 className="sub-heading">Broken Internal Links</h3>

            <div className="details-card">
              <p>
                <strong>Total Broken Links:</strong>{' '}
                {page?.brokenLinksCount ?? 0}
              </p>

              {page?.brokenLinks?.length > 0 ? (
                page.brokenLinks.map((link, index) => (
                  <div className="broken-link" key={index}>
                    <p><strong>URL:</strong> {link.url}</p>
                    <p><strong>Found On:</strong> {link.foundOnPage}</p>
                    <p><strong>Status Code:</strong> {link.statusCode}</p>
                  </div>
                ))
              ) : (
                <p className="success-message">
                  No broken internal links found.
                </p>
              )}
            </div>

            <h3 className="sub-heading">Gemini AI Recommendations</h3>

            <div className="recommendations">
              {result.recommendations || 'No recommendations available.'}
            </div>
          </section>
        )}
      </main>
    </div>
  )
}

export default App
