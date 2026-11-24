
import { useMemo, useState } from 'react'
import jsPDF from 'jspdf'
import './App.css'

function App() {
  const [url, setUrl] = useState('')
  const [result, setResult] = useState(null)
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState('')
  const [activeSection, setActiveSection] = useState('Overview')

  const pages = result?.pages || []
  const page = pages[0]
  const allMetrics = pages.flatMap((item) => item.seoMetrics || [])
  const allBrokenLinks = pages.flatMap((item) =>
    (item.brokenLinks || []).map((link) => ({
      ...link,
      sourcePage: link.foundOnPage || item.url,
    }))
  )

  const handleAudit = async (e) => {
    e.preventDefault()
    setLoading(true)
    setError('')
    setResult(null)
    setActiveSection('Overview')

    try {
      const response = await fetch('https://localhost:7082/api/Audit', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
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

  const downloadPDF = () => {
    if (!result) return

    const doc = new jsPDF()
    let y = 20

    const addText = (text, fontSize = 11) => {
      doc.setFontSize(fontSize)
      const lines = doc.splitTextToSize(String(text ?? ''), 170)
      const height = lines.length * 7 + 3

      if (y + height > 275) {
        doc.addPage()
        y = 20
      }

      doc.text(lines, 20, y)
      y += height
    }

    doc.setFont('helvetica', 'bold')
    addText('AI Marketing Intelligence', 18)
    addText('SEO Audit Report', 15)

    doc.setFont('helvetica', 'normal')
    addText(`Website: ${result.websiteUrl || url}`)
    addText(`Overall SEO Score: ${result.overallSeoScore ?? 0}/100`)
    addText(`Pages Crawled: ${result.totalPagesCrawled ?? pages.length}`)

    pages.forEach((item, index) => {
      addText(`Page ${index + 1}: ${item.url}`, 14)
      addText(`Title: ${item.pageTitle || 'Missing'}`)
      addText(`Meta Description: ${item.metaDescription || 'Missing'}`)
      addText(`H1 Count: ${item.h1Count ?? 0}`)
      addText(`Canonical URL: ${item.canonicalUrl || 'Missing'}`)
      addText(`JSON-LD Count: ${item.jsonLdCount ?? 0}`)

      item.seoMetrics?.forEach((metric) => {
        addText(`${metric.metricName}: ${metric.status} - ${metric.score} points`)
        addText(metric.details)
      })
    })

    addText('Broken Internal Links', 15)
    addText(`Total: ${allBrokenLinks.length}`)

    allBrokenLinks.forEach((link, index) => {
      addText(`${index + 1}. ${link.url}`)
      addText(`Status Code: ${link.statusCode}`)
      addText(`Found On: ${link.sourcePage}`)
    })

    addText('Gemini AI Recommendations', 15)
    addText(result.recommendations || 'No recommendations available.')

    doc.save('SEO-Audit-Report.pdf')
  }

  const navigation = [
    { name: 'Overview', icon: '⌂' },
    { name: 'SEO Metrics', icon: '▥' },
    { name: 'Crawled Pages', icon: '▤' },
    { name: 'Broken Links', icon: '↗' },
    { name: 'AI Insights', icon: '✧' },
  ]

  const score = Math.max(0, Math.min(100, Number(result?.overallSeoScore) || 0))

  return (
    <div className="dashboard">
      <aside className="sidebar">
        <div className="brand">
          <div className="brand-icon">✧</div>
          <div>
            <h2>AI Marketing</h2>
            <span>INTELLIGENCE ASSISTANT</span>
          </div>
        </div>

        <p className="nav-label">WORKSPACE</p>

        <nav className="navigation">
          {navigation.map((item) => (
            <button
              key={item.name}
              className={`nav-item ${activeSection === item.name ? 'active' : ''}`}
              onClick={() => setActiveSection(item.name)}
            >
              <span className="nav-icon">{item.icon}</span>
              {item.name}
            </button>
          ))}
        </nav>

        <div className="sidebar-bottom">
          <div className="sidebar-note">
            <span>✦</span>
            <p>AI-powered SEO analysis</p>
            <small>Understand your website. Improve your SEO.</small>
          </div>
        </div>
      </aside>

      <main className="main-content">
        <header className="topbar">
          <div>
            <span className="breadcrumb">Workspace</span>
            <span className="breadcrumb-separator"> / </span>
            <strong>{activeSection}</strong>
          </div>
          <div className="platform-status">
            <span className="online-dot"></span>
            SEO Analysis Platform
          </div>
        </header>

        <div className="page-content">
          <section className="welcome-section">
            <div>
              <span className="eyebrow">WEBSITE ANALYSIS TOOL</span>
              <h1>Discover your website's SEO health.</h1>
              <p>Analyze your website and get actionable AI-powered recommendations.</p>
            </div>

            <form className="audit-form" onSubmit={handleAudit}>
              <label htmlFor="website-url">Website URL</label>
              <div className="audit-input-row">
                <input
                  id="website-url"
                  type="url"
                  placeholder="https://example.com"
                  value={url}
                  onChange={(e) => setUrl(e.target.value)}
                  required
                />
                <button type="submit" disabled={loading}>
                  {loading ? 'Analyzing...' : '↗ Run Audit'}
                </button>
              </div>
            </form>
          </section>

          {error && <div className="error-message">{error}</div>}

          {loading && (
            <div className="loading-message">
              <span className="spinner"></span>
              Analyzing your website. Please wait...
            </div>
          )}

          {!result && !loading && (
            <section className="empty-state">
              <div className="empty-icon">✧</div>
              <h2>Ready to analyze your website?</h2>
              <p>Enter a website URL above and click Run Audit to see your SEO report.</p>
            </section>
          )}

          {result && (
            <>
              <div className="results-toolbar">
                <div>
                  <h2>{activeSection}</h2>
                  <p className="website-address">{result.websiteUrl || url}</p>
                </div>
                <button className="download-button" onClick={downloadPDF}>
                  ↓ Download PDF
                </button>
              </div>

              {activeSection === 'Overview' && (
                <>
                  <section className="overview-grid">
                    <div className="score-card">
                      <div>
                        <span className="card-label">OVERALL SEO SCORE</span>
                        <h2>{score}<small> / 100</small></h2>
                        <span className={`score-status ${score >= 70 ? 'good' : score >= 40 ? 'average' : 'poor'}`}>
                          {score >= 70 ? 'Good performance' : score >= 40 ? 'Needs improvement' : 'Needs attention'}
                        </span>
                      </div>
                      <div
                        className="score-ring"
                        style={{ '--score': `${score * 3.6}deg` }}
                      >
                        <div>{score}<small>/100</small></div>
                      </div>
                    </div>

                    <div className="stat-card">
                      <span className="stat-icon blue">▥</span>
                      <p>Pages Crawled</p>
                      <h2>{result.totalPagesCrawled ?? pages.length}</h2>
                    </div>

                    <div className="stat-card">
                      <span className="stat-icon purple">▤</span>
                      <p>SEO Checks</p>
                      <h2>{allMetrics.length}</h2>
                    </div>

                    <div className="stat-card">
                      <span className="stat-icon red">↗</span>
                      <p>Broken Links</p>
                      <h2>{allBrokenLinks.length}</h2>
                    </div>
                  </section>

                  <section className="overview-lower-grid">
                    <div className="panel">
                      <div className="panel-heading">
                        <h3>SEO Metrics Summary</h3>
                        <button onClick={() => setActiveSection('SEO Metrics')}>View all →</button>
                      </div>
                      {allMetrics.length > 0 ? allMetrics.slice(0, 5).map((metric, index) => (
                        <div className="metric-row" key={`${metric.metricName}-${index}`}>
                          <span className="metric-dot"></span>
                          <span>{metric.metricName}</span>
                          <strong className={metric.status?.toLowerCase() === 'pass' ? 'text-success' : 'text-danger'}>
                            {metric.status}
                          </strong>
                        </div>
                      )) : <p className="muted">No SEO metrics available.</p>}
                    </div>

                    <div className="panel ai-panel">
                      <div className="panel-heading">
                        <h3>✧ Gemini AI Insights</h3>
                        <button onClick={() => setActiveSection('AI Insights')}>View all →</button>
                      </div>
                      <p className="ai-preview">
                        {result.recommendations || 'No recommendations available.'}
                      </p>
                    </div>
                  </section>

                  <section className="overview-lower-grid">
                    <div className="panel">
                      <div className="panel-heading">
                        <h3>Recently Crawled Pages</h3>
                        <button onClick={() => setActiveSection('Crawled Pages')}>View all →</button>
                      </div>
                      {pages.slice(0, 4).map((item, index) => (
                        <div className="page-row" key={`${item.url}-${index}`}>
                          <span className="page-url">{item.url}</span>
                          <span className="page-tag">Crawled</span>
                        </div>
                      ))}
                    </div>

                    <div className="panel">
                      <div className="panel-heading">
                        <h3>Broken Links</h3>
                        <button onClick={() => setActiveSection('Broken Links')}>View all →</button>
                      </div>
                      {allBrokenLinks.length > 0 ? allBrokenLinks.slice(0, 4).map((link, index) => (
                        <div className="page-row" key={`${link.url}-${index}`}>
                          <span className="page-url">{link.url}</span>
                          <span className="broken-tag">{link.statusCode}</span>
                        </div>
                      )) : <p className="success-message">No broken internal links found.</p>}
                    </div>
                  </section>
                </>
              )}

              {activeSection === 'SEO Metrics' && (
                <section className="panel section-panel">
                  <h3>SEO Performance Checks</h3>
                  {pages.map((item, pageIndex) => (
                    <div key={item.url || pageIndex}>
                      <h4 className="page-section-title">{item.url}</h4>
                      <div className="metrics-grid">
                        {(item.seoMetrics || []).map((metric, index) => (
                          <article className="metric-card" key={`${metric.metricName}-${index}`}>
                            <div className="metric-card-header">
                              <h4>{metric.metricName}</h4>
                              <span className={`status-pill ${metric.status?.toLowerCase() === 'pass' ? 'pass' : 'fail'}`}>
                                {metric.status}
                              </span>
                            </div>
                            <strong className="metric-points">{metric.score} points</strong>
                            <p>{metric.details}</p>
                          </article>
                        ))}
                      </div>
                      <div className="details-list">
                        <h4>Page Details</h4>
                        <p><strong>Title:</strong> {item.pageTitle || 'Missing'}</p>
                        <p><strong>Title Length:</strong> {item.titleLength ?? 0} characters</p>
                        <p><strong>Meta Description:</strong> {item.metaDescription || 'Missing'}</p>
                        <p><strong>Meta Description Length:</strong> {item.metaDescriptionLength ?? 0} characters</p>
                        <p><strong>H1 Count:</strong> {item.h1Count ?? 0}</p>
                        <p><strong>Canonical URL:</strong> {item.canonicalUrl || 'Missing'}</p>
                        <p><strong>JSON-LD Count:</strong> {item.jsonLdCount ?? 0}</p>
                      </div>
                    </div>
                  ))}
                </section>
              )}

              {activeSection === 'Crawled Pages' && (
                <section className="panel section-panel">
                  <h3>Crawled Website Pages</h3>
                  <p className="muted">Total pages crawled: {result.totalPagesCrawled ?? pages.length}</p>
                  <div className="table-wrap">
                    <table>
                      <thead>
                        <tr><th>#</th><th>Page URL</th><th>SEO Checks</th><th>Status</th></tr>
                      </thead>
                      <tbody>
                        {pages.map((item, index) => (
                          <tr key={`${item.url}-${index}`}>
                            <td>{index + 1}</td>
                            <td className="table-url">{item.url}</td>
                            <td>{item.seoMetrics?.length ?? 0}</td>
                            <td><span className="page-tag">Crawled</span></td>
                          </tr>
                        ))}
                      </tbody>
                    </table>
                  </div>
                </section>
              )}

              {activeSection === 'Broken Links' && (
                <section className="panel section-panel">
                  <h3>Broken Internal Links</h3>
                  <p className="muted">Total broken links: {allBrokenLinks.length}</p>
                  {allBrokenLinks.length > 0 ? (
                    <div className="table-wrap">
                      <table>
                        <thead>
                          <tr><th>#</th><th>Broken URL</th><th>Found On Page</th><th>Status Code</th></tr>
                        </thead>
                        <tbody>
                          {allBrokenLinks.map((link, index) => (
                            <tr key={`${link.url}-${index}`}>
                              <td>{index + 1}</td>
                              <td className="table-url">{link.url}</td>
                              <td className="table-url">{link.sourcePage}</td>
                              <td><span className="broken-tag">{link.statusCode}</span></td>
                            </tr>
                          ))}
                        </tbody>
                      </table>
                    </div>
                  ) : (
                    <div className="empty-state compact">
                      <div className="empty-icon success-icon">✓</div>
                      <h3>No broken internal links found!</h3>
                      <p>Your crawled pages have no reported broken internal links.</p>
                    </div>
                  )}
                </section>
              )}

              {activeSection === 'AI Insights' && (
                <section className="panel section-panel ai-insights-panel">
                  <div className="ai-heading-icon">✧</div>
                  <h3>Gemini AI Recommendations</h3>
                  <p className="muted">AI-generated suggestions to improve your website's SEO performance.</p>
                  <div className="recommendations">
                    {result.recommendations || 'No recommendations available.'}
                  </div>
                  <p className="safety-note">ⓘ These are AI-generated recommendations. Your website content is not changed automatically.</p>
                </section>
              )}
            </>
          )}
        </div>
      </main>
    </div>
  )
}

export default App
